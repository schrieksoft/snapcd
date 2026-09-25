# snapcd job run

Runs an Apply against a Module with the runner answered in-process, and watches the job until it stops running.

Not a test. It asserts nothing: it starts a real job through the real services against a real database, and prints what happened. The value is the printout.

## What is real and what is not

Real: the DI composition (the server's own `AddSnapCd*` registrations plus the self-hosted edition policies), MassTransit over the SQL Server transport, the gatekeeper, the sagas, the consumers, the hub methods, runner authorization, and every write to the database.

Not real: the runner. `IHubContext<RunnerHub>` is replaced so dispatches land on `FakeRunner` instead of a socket, and each reply is made by calling the matching `RunnerHub` method directly. Nothing serializes, and no runner process, git repository or engine binary is involved.

So a green run says the server's own job path works end to end. It says nothing about what a real runner does, and a canned reply that is wrong in shape can stall the run where the real thing would not. That has already happened once: returning no output set leaves the saga in `OutputPending` forever, because the consumer publishes the saga's completion only in the branch that stores a set.

## Its own database

Each run creates a database, migrates it, applies the idempotent SQL, seeds it with `DebugDataSeeder` and drops it again. Nothing a server or another run holds is touched.

The bus lives in that same database, which is what keeps it isolated: a run pointed at the shared development database would share its bus with any server attached to it, and jobs would be picked up by that server's runners instead. Both connection strings are printed at startup so this is visible rather than assumed.

## Running it

```bash
dotnet run --project tools/SnapCd.JobRun -- \
    --server "Server=localhost,1435;User Id=sa;Password=...;TrustServerCertificate=True" \
    --verbose
```

`--server` carries no `Database=` entry; the run appends its own. Everything else defaults to what the seeder creates, so no ids need supplying. `--keep` leaves the database behind to inspect, `--database` names it, `--timeout` bounds the watch.

## What it prints

The two databases, the Module and runner, then the status each time it changes, and the steps that were dispatched in order. On a job that ended badly, the server-side step it failed on with the first line of the error. A run that times out prints the last status it saw, and the dispatched list shows which step it was sitting on.

A green Apply dispatches nine steps:

```
Ping GetDefinitiveRevision GetModule Init Validate Input Plan ApplyFromPlan Output
```

`Ping` is the liveness probe, and a connection that does not answer it is dropped as stale before any step is dispatched. A real runner also reports its current task on a timer, which is what the heartbeat reads; reporting once per step is not enough, and a job that idles past the threshold is failed as abandoned.
