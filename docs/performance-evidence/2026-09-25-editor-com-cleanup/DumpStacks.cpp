#include <windows.h>
#include <dbgeng.h>
#include <cstdio>
class OutputSink : public IDebugOutputCallbacks {
public:
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (iid == __uuidof(IUnknown) || iid == __uuidof(IDebugOutputCallbacks)) { *result = this; return S_OK; }
        *result = nullptr; return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return 1; }
    ULONG STDMETHODCALLTYPE Release() override { return 1; }
    HRESULT STDMETHODCALLTYPE Output(ULONG, PCSTR text) override { std::fputs(text, stdout); std::fflush(stdout); return S_OK; }
};
int main(int argc, char** argv) {
    if (argc != 4) return 2;
    IDebugClient* client = nullptr;
    IDebugControl* control = nullptr;
    IDebugSymbols* symbols = nullptr;
    HRESULT hr = DebugCreate(__uuidof(IDebugClient), reinterpret_cast<void**>(&client));
    if (FAILED(hr)) return 3;
    OutputSink output;
    client->SetOutputCallbacks(&output);
    client->QueryInterface(__uuidof(IDebugControl), reinterpret_cast<void**>(&control));
    client->QueryInterface(__uuidof(IDebugSymbols), reinterpret_cast<void**>(&symbols));
    symbols->SetSymbolPath(argv[2]);
    hr = client->OpenDumpFile(argv[1]);
    if (SUCCEEDED(hr)) hr = control->WaitForEvent(0, INFINITE);
    if (SUCCEEDED(hr)) hr = control->Execute(DEBUG_OUTCTL_THIS_CLIENT, argv[3], DEBUG_EXECUTE_DEFAULT);
    std::printf("\nDebugger result: %08lx\n", hr);
    client->EndSession(DEBUG_END_PASSIVE);
    symbols->Release(); control->Release(); client->Release();
    return FAILED(hr) ? 1 : 0;
}
