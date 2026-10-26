package main

import (
	"context"
	"encoding/json"
	"flag"
	"fmt"
	"os"

	"github.com/opentofu/opentofu/internal/backend"
	"github.com/opentofu/opentofu/internal/backend/local"
	"github.com/opentofu/opentofu/internal/command/arguments"
	"github.com/opentofu/opentofu/internal/command/clistate"
	"github.com/opentofu/opentofu/internal/command/views"
	"github.com/opentofu/opentofu/internal/configs"
	"github.com/opentofu/opentofu/internal/configs/configload"
	"github.com/opentofu/opentofu/internal/depsfile"
	"github.com/opentofu/opentofu/internal/encryption"
	"github.com/opentofu/opentofu/internal/plans"
	"github.com/opentofu/opentofu/internal/plugins"
	"github.com/opentofu/opentofu/internal/terminal"
	"github.com/opentofu/opentofu/internal/tfdiags"
	"github.com/opentofu/opentofu/internal/tofu"

	"github.com/opentofu/opentofu/snapcdinspect/signals"
)

// runProbe plans the module in dir with another root's predicted values supplied
// for its variables, and writes what that does to its own outputs.
//
// A predicted value may be unknown, which is the point: it carries that a value
// will change without claiming what it becomes. No variable file can express
// that, so the values are handed to the plan as cty values directly, which is
// what tofu does internally for every variable source.
//
// The plan is read and discarded. Nothing applies it, so no plan file is written.
func runProbe(args []string) int {
	flags := flag.NewFlagSet("probe", flag.ExitOnError)
	dir := flags.String("dir", ".", "module directory to plan, already initialized")
	signalsIn := flags.String("signals-in", "", "path to a signals document of predicted values")
	signalsOut := flags.String("signals-out", "", "path to write this module's predicted outputs to; - for stdout")
	varFile := flags.String("var-file", "", "optional variable file, as tofu -var-file")
	if err := flags.Parse(args); err != nil {
		return 1
	}

	if *signalsOut == "" {
		fmt.Fprintln(os.Stderr, "usage: snapcd-inspect probe --dir <path> --signals-in <path> --signals-out <path> [--var-file <path>]")
		return 1
	}

	if err := os.Chdir(*dir); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	predicted, err := loadPredicted(*signalsIn)
	if err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	plan, diags := planWithPredicted(predicted, *varFile)
	if diags.HasErrors() {
		fmt.Fprintf(os.Stderr, "%s\n", diags.Err())
		return 1
	}

	changes, err := signals.OutputChangesFor(plan)
	if err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	if err := signals.WriteOutputChanges(*signalsOut, changes); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	return 0
}

// loadPredicted reads a signals document and presents each variable it carries
// as a value the plan can take, unknowns and refinements intact.
func loadPredicted(path string) (map[string]backend.UnparsedVariableValue, error) {
	values := map[string]backend.UnparsedVariableValue{}
	if path == "" {
		return values, nil
	}

	raw, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read signals: %w", err)
	}

	var in signals.Inputs
	if err := json.Unmarshal(raw, &in); err != nil {
		return nil, fmt.Errorf("parse signals: %w", err)
	}

	if in.FormatVersion != signals.FormatVersion {
		return nil, fmt.Errorf(
			"unsupported signals format version %q; only %q is supported",
			in.FormatVersion, signals.FormatVersion)
	}

	for name, value := range in.Variables {
		values[name] = signals.PredictedValue{Name: name, Value: value}
	}

	return values, nil
}

// planWithPredicted runs tofu's own plan over the current directory, with the
// predicted values layered over every other variable source.
func planWithPredicted(
	predicted map[string]backend.UnparsedVariableValue,
	varFile string,
) (*plans.Plan, tfdiags.Diagnostics) {
	var diags tfdiags.Diagnostics
	ctx := context.Background()

	loader, err := configload.NewLoader(&configload.Config{ModulesDir: ".terraform/modules"})
	if err != nil {
		return nil, diags.Append(fmt.Errorf("config loader: %w", err))
	}

	// The probe runs in a directory the job already inited, so the provider
	// selections are the ones that init recorded.
	locks, lockDiags := depsfile.LoadLocksFromFile(".terraform.lock.hcl")
	diags = diags.Append(lockDiags)
	if diags.HasErrors() {
		return nil, diags
	}

	// A directory Terraform initialised records its providers under Terraform's
	// registry; the engine resolves the same sources to its own. Re-key the locks
	// so the engine's consistency check passes, and remember where the packages
	// actually sit.
	locks, onDisk := retargetLocks(locks)

	factories, err := providerFactories(locks, ".terraform/providers", onDisk)
	if err != nil {
		return nil, diags.Append(err)
	}

	// The same plan rendering tofu's own plan prints, so a probe's logs read like
	// any other step's rather than being silent on success.
	streams, err := terminal.Init()
	if err != nil {
		return nil, diags.Append(fmt.Errorf("terminal: %w", err))
	}
	// Colour as tofu's own plan does, so a probe's log reads like the Plan step
	// beside it rather than as flat text. A View colorizes only once Configure has
	// been applied to it, which NewPlan does not do.
	viewArgs := &arguments.View{ViewType: arguments.ViewHuman}
	view := views.NewView(streams)
	view.Configure(viewArgs)
	planView := views.NewPlan(viewArgs, view)

	enc := encryption.Disabled()

	// A probe plans against the module's real state, so it reads from whatever
	// backend init configured. The operation still runs locally; the backend
	// supplies state and nothing else.
	remote, backendDiags := stateBackend(ctx, enc.State())
	diags = diags.Append(backendDiags)
	if diags.HasErrors() {
		return nil, diags
	}

	var be *local.Local
	if remote != nil {
		be = local.NewWithBackend(remote, enc.State())
	} else {
		be = local.New(enc.State())
	}
	// No provisioners: a probe plans and never applies, so no provisioner runs.
	be.ContextOpts = &tofu.ContextOpts{
		Plugins: plugins.NewLibrary(factories, nil),
	}

	fileVars, err := varsFromFile(varFile)
	if err != nil {
		return nil, diags.Append(err)
	}

	// The predicted values outrank every other source, as they do in the CLI.
	collected := map[string]backend.UnparsedVariableValue{}
	for name, value := range fileVars {
		collected[name] = value
	}
	for name, value := range predicted {
		collected[name] = value
	}

	opReq := &backend.Operation{
		Type:         backend.OperationTypePlan,
		ConfigDir:    ".",
		ConfigLoader: loader,
		PlanRefresh:  false,
		Workspace:    backend.DefaultStateName,
		Encryption:   enc,
		Variables:    collected,
		// A probe reads a plan and discards it, so it takes no lock.
		StateLocker:     clistate.NewNoopLocker(),
		DependencyLocks: locks,
		View:            planView.Operation(),
	}

	run, _, runDiags := be.LocalRun(ctx, ctx, opReq)
	diags = diags.Append(runDiags)
	if diags.HasErrors() {
		return nil, diags
	}

	// LocalRun parsed the collected sources into the run's options; re-apply the
	// predicted ones so an unknown is not displaced by a file value.
	for name, value := range predicted {
		parsed, parseDiags := value.ParseVariableValue(configs.VariableParseHCL)
		diags = diags.Append(parseDiags)
		if parseDiags.HasErrors() {
			return nil, diags
		}
		run.PlanOpts.SetVariables[name] = parsed
	}

	plan, planDiags := run.Core.Plan(ctx, run.Config, run.InputState, run.PlanOpts)
	diags = diags.Append(planDiags)
	if diags.HasErrors() {
		return nil, diags
	}

	// The backend renders the plan as part of its own plan operation, which this
	// does not use, so render it here instead: a probe's logs show the plan it
	// made, the same way every other step shows its tool's output.
	schemas, schemaDiags := run.Core.Schemas(ctx, run.Config, run.InputState)
	if !schemaDiags.HasErrors() {
		planView.Operation().Plan(plan, schemas)
	}

	return plan, diags
}
