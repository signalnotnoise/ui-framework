"""Read handle types from Windows minidumps and compare SOS heap statistics; no budget claims."""
import argparse
from collections import Counter
import json
import mmap
from pathlib import Path
import re
import struct


def handles(data, include_records=False):
    def unpack(fmt, offset):
        if offset < 0 or offset + struct.calcsize(fmt) > len(data):
            raise ValueError('Truncated minidump metadata')
        return struct.unpack_from(fmt, data, offset)

    if bytes(data[:4]) != b'MDMP':
        raise ValueError('Not a Windows minidump')
    streams, directory = unpack('<II', 8)
    if directory + streams * 12 > len(data):
        raise ValueError('Truncated minidump stream directory')
    for index in range(streams):
        kind, size, offset = unpack('<III', directory + 12 * index)
        if kind != 12:  # HandleDataStream in MINIDUMP_STREAM_TYPE.
            continue
        header, descriptor, count, _ = unpack('<IIII', offset)
        if header < 16 or descriptor not in (32, 40) or header + count * descriptor > size:
            raise ValueError('Invalid handle stream layout')
        result = Counter()
        records = []
        for item in range(count):
            handle, name_rva = unpack('<QI', offset + header + item * descriptor)
            name = '<unknown>'
            if name_rva:
                length, = unpack('<I', name_rva)
                if length % 2 or name_rva + 4 + length > len(data):
                    raise ValueError('Invalid handle type string')
                name = bytes(data[name_rva + 4:name_rva + 4 + length]).decode('utf-16-le')
            result[name] += 1
            if include_records:
                records.append(dict(handle=f'0x{handle:x}', type=name))
        captured = dict(available=True, count=count, types=dict(sorted(result.items())))
        if include_records:
            captured['records'] = records
        return captured
    return dict(available=False, reason='No handle data stream; not evidence of zero handles')


def heap_stats(text):
    result = {}
    # dumpheap -stat stops at its Total line; exclude later GC-handle statistics.
    for line in text.splitlines():
        if line.startswith('Total '):
            break
        match = re.match(r'^\s*([0-9a-fA-F]{8,16})\s+([\d,]+)\s+([\d,]+)\s+(.+?)\s*$', line)
        if match:
            _, count, size, name = match.groups()
            old = result.setdefault(name, dict(count=0, bytes=0))
            old['count'] += int(count.replace(',', ''))
            old['bytes'] += int(size.replace(',', ''))
    if not result:
        raise ValueError('Missing SOS heap statistics')
    return result


def thread_names(data):
    """MINIDUMP_THREAD_NAME_LIST uses packed 12-byte (DWORD, RVA64) records."""
    def unpack(fmt, offset):
        if offset < 0 or offset + struct.calcsize(fmt) > len(data):
            raise ValueError('Truncated thread-name metadata')
        return struct.unpack_from(fmt, data, offset)
    streams, directory = unpack('<II', 8)
    for index in range(streams):
        kind, size, offset = unpack('<III', directory + 12 * index)
        if kind != 24:
            continue
        count, = unpack('<I', offset)
        if 4 + count * 12 > size:
            raise ValueError('Invalid thread-name stream')
        names = {}
        for item in range(count):
            tid, rva = unpack('<IQ', offset + 4 + item * 12)
            length, = unpack('<I', rva)
            if length % 2 or rva + 4 + length > len(data):
                raise ValueError('Invalid thread name')
            names[str(tid)] = bytes(data[rva + 4:rva + 4 + length]).decode('utf-16-le')
        return dict(available=True, names=names)
    return dict(available=False, names={})


def summarize(directory, include_records=False):
    directory = Path(directory)
    phases = {}
    for phase in ('start', 'end', 'quiescent'):
        with (directory / f'{phase}.dmp').open('rb') as stream:
            with mmap.mmap(stream.fileno(), 0, access=mmap.ACCESS_READ) as data:
                captured = handles(data, include_records)
                names = thread_names(data)
        phases[phase] = dict(handles=captured, threadNames=names, heap=heap_stats((directory / f'{phase}.analysis.txt').read_text(encoding='utf-8-sig')))
    comparisons = {}
    for phase in ('end', 'quiescent'):
        before, after = phases['start']['heap'], phases[phase]['heap']
        changes = [dict(type=name, count=after.get(name, {}).get('count', 0) - before.get(name, {}).get('count', 0),
                        bytes=after.get(name, {}).get('bytes', 0) - before.get(name, {}).get('bytes', 0))
                   for name in before.keys() | after.keys()]
        comparisons[phase] = sorted((x for x in changes if x['count'] or x['bytes']), key=lambda x: abs(x['bytes']), reverse=True)
    history = {}
    if include_records and phases['start']['handles']['available']:
        before = {x['handle']: x['type'] for x in phases['start']['handles']['records']}
        for phase in ('end', 'quiescent'):
            path = directory / f'{phase}.handles.json'
            if not path.exists():
                continue
            trace = json.loads(path.read_text(encoding='utf-8-sig'))
            latest = {}
            for entry in sorted(trace['Traces'], key=lambda x: x['Index']):
                latest.setdefault(entry['Handle'], entry)
            files = {x['Handle']: x for x in trace.get('Files', [])}
            added = []
            for record in phases[phase]['handles'].get('records', []):
                handle = record['handle']
                if before.get(handle) == record['type']:
                    continue
                entry = latest.get(handle)
                added.append(dict(**record, latestOperation=entry,
                    checkpointThreadName=None if entry is None else phases[phase]['threadNames']['names'].get(str(entry['ThreadId'])),
                    fileIdentity=files.get(handle)))
            history[phase] = dict(totalTraces=trace['TotalTraces'], ringMayHaveWrapped=trace['RingMayHaveWrapped'],
                                  addedHandleValues=added,
                                  limitation='Values may be reused; new values are not net handle growth. Latest operation is newest-first history. A close or missing trace cannot establish creation ownership. Thread names are from the checkpoint, not creation stacks.')
    return dict(protocol='memory-attribution-paused-v1', releaseGate=False, phases=phases, heapChanges=comparisons, handleHistory=history,
                limitation='Dump pauses perturb the process. Heap totals include unreachable objects unless roots are verified; handle type counts do not establish ownership.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory')
    parser.add_argument('--output', required=True)
    parser.add_argument('--handle-records', action='store_true', help='Include handle values/types for matching bounded creation history; values can be reused.')
    args = parser.parse_args()
    with Path(args.output).open('x', encoding='utf-8') as output:
        json.dump(summarize(args.directory, args.handle_records), output, indent=2)
        output.write('\n')
