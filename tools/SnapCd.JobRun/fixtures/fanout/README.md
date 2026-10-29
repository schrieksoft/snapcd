# The fanout

Four roots arranged so that `a` feeds `b`, `c` and `d`, and every consumer answers. Where the diamond exercises how far a wave travels, this exercises what the approval panel says about each participant once it arrives: the three consumers answer the three ways a probe can report.

| Root | Reads | Reports | The case it covers |
|---|---|---|---|
| probe-fanout-a | - | `a_id` changes | origin |
| probe-fanout-b | `a_id` | two resources updated, no output moves | changes without signalling |
| probe-fanout-c | `a_id` | three outputs, all no-ops, no resources | unaffected, reported as signals |
| probe-fanout-d | `a_id` | nothing at all | an empty reply |

## What each one is for

**b** changes resources while passing nothing on. `null_resource.b_first` and `module.inner.null_resource.b_second` are updated in place when `a_id` moves, and `b_id` is published from neither, so it resolves to what it already was. A panel counting only output signals reads this as "no change" while the module's own log shows two updates. The second resource sits in a child module, so a reader is also told where a change lives.

**c** is genuinely unaffected: nothing reads `from_a`. A probe still reports its three outputs, because a consumer needs a value for every variable it reads, and all three come back as no-ops. A panel counting signal rows reads this as "changes".

**d** answers with an empty reply. An unrecorded attempt used to leave the module reading as still running on a job that had already completed, with the wave never settling.

`a` declares `generation = "2"` while its state holds the pet created under `generation = "1"`. `keepers` forces replacement, so `a_id` becomes unknown: a value no plan can predict.

The directories are named as the debug seeder names the Modules, because the semi-fake runner matches them on the Module's source subdirectory.

## State

These roots have no checked-in state, unlike the diamond's. The fake runner replays each one's reply from `FanoutSignals`, so no plan runs. A `--real-runner` run against this fixture would need state applying first.
