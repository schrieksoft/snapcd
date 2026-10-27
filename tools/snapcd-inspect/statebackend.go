package main

// Builds the state backend a probe plans against, from what `init` recorded.
//
// OpenTofu does this in Meta.backendFromConfig, which is unexported, so the
// wiring is reproduced here: read the backend `init` wrote into the data dir,
// construct that backend type, and configure it with the same values. The
// backend supplies state only; the operation still runs locally, which is what
// local.NewWithBackend expresses.

import (
	"context"
	"fmt"
	"path/filepath"

	"github.com/opentofu/opentofu/internal/backend"
	backendInit "github.com/opentofu/opentofu/internal/backend/init"
	"github.com/opentofu/opentofu/internal/command/arguments"
	"github.com/opentofu/opentofu/internal/command/clistate"
	"github.com/opentofu/opentofu/internal/encryption"
	"github.com/opentofu/opentofu/internal/tfdiags"
)

// dataDir is where `init` records what it configured.
const dataDir = ".terraform"

// stateBackend returns the backend holding this module's state, or nil when the
// module keeps state in a local file.
func stateBackend(ctx context.Context, enc encryption.StateEncryption) (backend.Backend, tfdiags.Diagnostics) {
	var diags tfdiags.Diagnostics

	recorded, diags := recordedBackend(ctx)
	if diags.HasErrors() || recorded == nil {
		return nil, diags
	}

	backendInit.Init(nil)

	newBackend, _ := backendInit.Backend(recorded.Type)
	if newBackend == nil {
		return nil, diags.Append(fmt.Errorf("unknown backend type %q", recorded.Type))
	}

	be := newBackend(enc)

	config, err := recorded.Config(be.ConfigSchema())
	if err != nil {
		return nil, diags.Append(fmt.Errorf(
			"reading the recorded %q backend configuration: %w", recorded.Type, err))
	}

	prepared, prepareDiags := be.PrepareConfig(config)
	diags = diags.Append(prepareDiags)
	if diags.HasErrors() {
		return nil, diags
	}

	diags = diags.Append(be.Configure(ctx, prepared))
	if diags.HasErrors() {
		return nil, diags
	}

	return be, diags
}

// recordedBackend reads the backend `init` wrote, returning nil when the
// directory was never initialized against one.
func recordedBackend(ctx context.Context) (*clistate.BackendState, tfdiags.Diagnostics) {
	var diags tfdiags.Diagnostics

	path := filepath.Join(dataDir, arguments.DefaultStateFilename)
	mgr := &clistate.LocalState{Path: path}

	if err := mgr.RefreshState(ctx); err != nil {
		return nil, diags.Append(fmt.Errorf("reading %s: %w", path, err))
	}

	state := mgr.State()
	if state == nil || state.Backend.Empty() {
		return nil, diags
	}

	return state.Backend, diags
}
