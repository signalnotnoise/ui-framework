# Rendering failures and recovery

ViewHost has two failure boundaries:

- A body-build or validation exception leaves that host's previous tree and dependency subscriptions intact. Fix the input and call `Refresh()`, or allow a previously observed state to invalidate it.
- Once patching starts, an exception can follow arbitrary native or application mutations. The host clears its content and disposes the damaged tree instead of reusing it. All framework callbacks on detached controls are inactive. Automatic refresh is suspended until an explicit successful `Refresh()` builds a fresh tree. Disposal remains supported. Component-local state in the discarded tree is lost; durable state belongs in application models.

`Refresh()` propagates the original failure. Cleanup failures are aggregated with the original failure. A failed nested host is cleared at its own boundary; if its exception propagates through a parent's active patch, that parent's boundary also clears its tree. Exceptions from dispatcher-driven refresh still reach WPF's dispatcher exception handling; applications choose how to report them and when to retry. There is no built-in error UI, automatic retry loop, or rollback of arbitrary application side effects. Exceptions raised later by WPF layout or templates outside Refresh are outside this boundary.

Body builds and render/cleanup callbacks must not synchronously reenter `Refresh()` or dispose the host currently rendering. Such reentrancy throws. Schedule application work after the active callback returns.

Public View records remain supported. The WPF renderer validates dimensions, editor limits, enum values, child structure, component/platform metadata and sibling keys before patching a host. Component bodies undergo validation when their own hosts build. Color parsing and application/native callbacks can still fail during patching.

Observable state belongs to its creating thread. Derived ObservableState implementations must call VerifyAccess before mutating their fields; NotifyChanged also verifies access before dispatching notifications. State and Computed notifications invoke every subscriber in the captured delegate, even if an earlier one throws. A single error is rethrown with its stack; multiple failures become an AggregateException. Mutations have already happened when notification errors are reported. Subscription changes take effect on the next notification. This is synchronous delivery, not thread marshaling.

StateList enumeration intentionally takes a stable array snapshot. Mutation during enumeration does not invalidate an existing enumerator. ReplaceAll materializes input before mutating, including self-enumeration. These allocations are part of the correctness contract; changing to a fail-fast enumerator would be an API behavior change.

Regression coverage lives in RendererFailureTests, ViewValidationTests, StateNotificationTests and NativeHostTests. The STA runner terminates the isolated test host after a 60-second test timeout, with the test name, thread identity/state and elapsed time. The release command retains its additional two-minute test-runner hang diagnostic. Tests pump dispatcher work explicitly so scheduling order remains under test control.
