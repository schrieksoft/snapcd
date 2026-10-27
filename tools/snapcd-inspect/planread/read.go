package planread

import (
	"archive/zip"
	"fmt"
	"io"

	"github.com/opentofu/opentofu/internal/addrs"
	"github.com/opentofu/opentofu/internal/plans"

	"github.com/opentofu/opentofu/snapcdinspect/planproto"
)

// A plan file is a zip whose "tfplan" entry holds the plan as protobuf.
const planEntryName = "tfplan"

// The format version this decoder understands. A plan file states its own
// version, which is a property of the file rather than of the tool that wrote
// it, so it is still checked.
const planFormatVersion = 3

// OutputChanges returns what a saved plan predicts for its root module's
// outputs, without reconstructing the rest of the plan.
func OutputChanges(planPath string) ([]*plans.OutputChangeSrc, error) {
	archive, err := zip.OpenReader(planPath)
	if err != nil {
		return nil, fmt.Errorf("read plan file: %w", err)
	}
	defer archive.Close()

	for _, file := range archive.File {
		if file.Name != planEntryName {
			continue
		}

		entry, err := file.Open()
		if err != nil {
			return nil, fmt.Errorf("read plan file: %w", err)
		}
		defer entry.Close()

		return outputChangesFrom(entry)
	}

	return nil, fmt.Errorf("%s does not contain a %q entry", planPath, planEntryName)
}

func outputChangesFrom(r io.Reader) ([]*plans.OutputChangeSrc, error) {
	src, err := io.ReadAll(r)
	if err != nil {
		return nil, err
	}

	rawPlan, err := planproto.UnmarshalPlan(src)
	if err != nil {
		return nil, fmt.Errorf("parse error: %w", err)
	}

	if rawPlan.Version != planFormatVersion {
		return nil, fmt.Errorf(
			"unsupported plan file format version %d; only version %d is supported",
			rawPlan.Version, planFormatVersion)
	}

	changes := make([]*plans.OutputChangeSrc, 0, len(rawPlan.OutputChanges))
	for _, rawOC := range rawPlan.OutputChanges {
		change, err := changeFromTfplan(rawOC.Change)
		if err != nil {
			return nil, fmt.Errorf("invalid plan for output %q: %w", rawOC.Name, err)
		}

		changes = append(changes, &plans.OutputChangeSrc{
			Addr:      addrs.OutputValue{Name: rawOC.Name}.Absolute(addrs.RootModuleInstance),
			ChangeSrc: *change,
			Sensitive: rawOC.Sensitive,
		})
	}

	return changes, nil
}
