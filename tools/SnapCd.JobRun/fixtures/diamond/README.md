# The diamond, as checked-in state

Six OpenTofu roots with their applied state, arranged so that planning `a` from these files alone produces a cascade. Nothing here needs setting up: `tofu init` and the plan is a replacement.

| Root | Reads | Publishes | Reached by a plan of `a`? |
|---|---|---|---|
| probe-diamond-a | - | `a_id` (changes), `a_other` (does not) | origin |
| probe-diamond-b | `a_id` | `b_id` (changes), `b_other` (does not) | yes |
| probe-diamond-c | `a_id` | `c_id` (changes) | yes |
| probe-diamond-d | `b_id` and `c_id` | `d_id` | yes, after both |
| probe-diamond-e | `a_other` | `e_id` | no |
| probe-diamond-f | `b_other` | `f_id` | no |

## What makes it a change

`probe-diamond-a/main.tf` declares `generation = "2"` while `terraform.tfstate` holds the pet created under `generation = "1"`. `keepers` forces replacement, so the plan destroys and recreates the pet and `a_id` becomes unknown: a value no plan can predict.

Each root also publishes a second output from a resource whose keepers never change, so that output is a no-op. `e` and `f` read only those, which is what makes them control cases: a propagation that reaches them has over-reached.

The directories are named as the debug seeder names the Modules, because the semi-fake runner matches them on the Module's source subdirectory.

## Why the state is committed

Without it every root plans as a fresh create, every output is an unknown creation, and the fixture proves nothing: there is no prior value for a change to differ from and no no-op to leave alone.

`.terraform/` is not committed, so a run does a real `tofu init` first. The lock files are, so init pins the provider version the state was created with.
