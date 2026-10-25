package main

import (
	"flag"
	"fmt"
	"os"

	"github.com/opentofu/opentofu/snapcdinspect/planread"
	"github.com/opentofu/opentofu/snapcdinspect/signals"
)

// runSignals reads a saved plan file and writes what it predicts for the root
// module's outputs.
//
// Only the output changes are read, not the whole plan: a plan can only be
// applied by the exact build that wrote it, because some parts have to
// round-trip precisely. An output change is a cty value in cty's own encoding,
// which is stable, so one build can report on a plan another build wrote.
func runSignals(args []string) int {
	flags := flag.NewFlagSet("signals", flag.ExitOnError)
	planPath := flags.String("plan", "", "path to a saved plan file")
	out := flags.String("out", "", "path to write the signals document to; - for stdout")
	if err := flags.Parse(args); err != nil {
		return 1
	}

	if *planPath == "" || *out == "" {
		fmt.Fprintln(os.Stderr, "usage: snapcd-inspect signals --plan <path> --out <path>")
		return 1
	}

	outputChanges, err := planread.OutputChanges(*planPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	changes, err := signals.OutputChangesFrom(outputChanges)
	if err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	if err := signals.WriteOutputChanges(*out, changes); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		return 1
	}

	return 0
}
