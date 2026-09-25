#include <windows.h>
#include <unknwn.h>
#include <atomic>
static std::atomic<int> active{0}, wrongThread{0};
class Probe final : public IUnknown {
    std::atomic<ULONG> references{1};
    DWORD owner = GetCurrentThreadId();
public:
    Probe() { ++active; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid != IID_IUnknown) return E_NOINTERFACE;
        *result = this; AddRef(); return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++references; }
    ULONG STDMETHODCALLTYPE Release() override {
        ULONG remaining = --references;
        if (!remaining) {
            if (GetCurrentThreadId() != owner) ++wrongThread;
            --active; delete this;
        }
        return remaining;
    }
};
extern "C" __declspec(dllexport) IUnknown* __cdecl CreateProbe() { return new Probe(); }
extern "C" __declspec(dllexport) int __cdecl ActiveProbes() { return active.load(); }
extern "C" __declspec(dllexport) int __cdecl WrongThreadReleases() { return wrongThread.load(); }
