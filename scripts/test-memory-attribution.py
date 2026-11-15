"""Verify handle-type accounting and reject absent/truncated diagnostic evidence."""
import importlib.util
from pathlib import Path
import struct
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('attribution', Path(__file__).resolve().parents[1] / 'tools/Summarize-MemoryAttribution.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
for descriptor in (32, 40):
    data = bytearray(512)
    data[:4] = b'MDMP'
    struct.pack_into('<II', data, 8, 1, 32)
    struct.pack_into('<III', data, 32, 12, 16 + 3 * descriptor, 64)
    struct.pack_into('<IIII', data, 64, 16, descriptor, 3, 0)
    for index in range(3):
        struct.pack_into('<QI', data, 80 + index * descriptor, index + 1, 256 if index < 2 else 0)
    label = 'Event'.encode('utf-16-le')
    struct.pack_into('<I', data, 256, len(label))
    data[260:260 + len(label)] = label
    assert module.handles(data) == dict(available=True, count=3, types={'<unknown>': 1, 'Event': 2})
    assert module.handles(data, True)['records'][0] == {'handle': '0x1', 'type': 'Event'}
    try: module.handles(data[:100])
    except ValueError: pass
    else: raise AssertionError('Truncated handle data must not produce a successful count')
struct.pack_into('<I', data, 8, 0)
assert not module.handles(data)['available']
stats = module.heap_stats(' 00007fff12345678 1,024 32,768 System.Byte[]\nTotal 1024 objects\n 00007fff12345678 1 5 NotHeap\n')
assert stats == {'System.Byte[]': {'count': 1024, 'bytes': 32768}}
assert not module.thread_names(data)['available']
struct.pack_into('<II', data, 8, 1, 32)
struct.pack_into('<III', data, 32, 24, 16, 64)
struct.pack_into('<I', data, 64, 1)
struct.pack_into('<IQ', data, 68, 123, 256)
assert module.thread_names(data) == {'available': True, 'names': {'123': 'Event'}}
print('PASS: both handle descriptor layouts, unknown types, missing/truncated streams and SOS statistic boundaries.')
print('PASS: handle identities and packed thread-name stream; missing names remain unavailable.')
