// MSTest runs sequentially unless told otherwise. The test classes share no state — each one
// builds its own repository, hub harness and substitutes — so they are safe to run side by side.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
