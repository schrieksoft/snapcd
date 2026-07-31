// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

package main

import (
	"os"
	"os/exec"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

// buildRepo creates a git repository from a map of relative file path -> contents and returns the repo dir and
// head commit SHA.
func buildRepo(t *testing.T, files map[string]string) (string, string) {
	t.Helper()
	dir := t.TempDir()

	for name, contents := range files {
		full := filepath.Join(dir, name)
		if err := os.MkdirAll(filepath.Dir(full), 0o755); err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(full, []byte(contents), 0o644); err != nil {
			t.Fatal(err)
		}
	}

	git := func(args ...string) string {
		t.Helper()
		cmd := exec.Command("git", append([]string{"-C", dir}, args...)...)
		cmd.Env = append(os.Environ(),
			"GIT_AUTHOR_NAME=t", "GIT_AUTHOR_EMAIL=t@t", "GIT_COMMITTER_NAME=t", "GIT_COMMITTER_EMAIL=t@t")
		output, err := cmd.CombinedOutput()
		if err != nil {
			t.Fatalf("git %v: %v: %s", args, err, output)
		}
		return strings.TrimSpace(string(output))
	}

	git("init", "-q", "-b", "main")
	git("add", "-A")
	git("commit", "-q", "-m", "fixture")
	return dir, git("rev-parse", "HEAD")
}

func closures(t *testing.T, files map[string]string, roots ...string) map[string]Closure {
	t.Helper()
	dir, commit := buildRepo(t, files)
	result, err := run(dir, commit, roots)
	if err != nil {
		t.Fatal(err)
	}
	byRoot := map[string]Closure{}
	for _, c := range result.Closures {
		byRoot[c.RootPath] = c
	}
	return byRoot
}

func moduleCall(name, source string) string {
	return "module \"" + name + "\" {\n  source = \"" + source + "\"\n}\n"
}

func assertPaths(t *testing.T, label string, got, want []string) {
	t.Helper()
	if len(got) == 0 && len(want) == 0 {
		return
	}
	if !reflect.DeepEqual(got, want) {
		t.Errorf("%s: got %v, want %v", label, got, want)
	}
}

func TestNestedChainIsTransitive(t *testing.T) {
	result := closures(t, map[string]string{
		"modules/app/main.tf":    moduleCall("network", "../../shared/network"),
		"shared/network/main.tf": moduleCall("naming", "../naming"),
		"shared/naming/main.tf":  "output \"prefix\" { value = \"x\" }\n",
	}, "modules/app")

	assertPaths(t, "referenced", result["modules/app"].ReferencedPaths, []string{"shared/naming", "shared/network"})
	assertPaths(t, "dangling", result["modules/app"].DanglingPaths, nil)
}

func TestDiamondIsDeduplicated(t *testing.T) {
	result := closures(t, map[string]string{
		"root/main.tf":   moduleCall("a", "../a") + moduleCall("b", "../b"),
		"a/main.tf":      moduleCall("common", "../common"),
		"b/main.tf":      moduleCall("common", "../common"),
		"common/main.tf": "output \"x\" { value = 1 }\n",
	}, "root")

	assertPaths(t, "referenced", result["root"].ReferencedPaths, []string{"a", "b", "common"})
}

func TestCycleTerminates(t *testing.T) {
	result := closures(t, map[string]string{
		"a/main.tf": moduleCall("b", "../b"),
		"b/main.tf": moduleCall("a", "../a"),
	}, "a")

	assertPaths(t, "referenced", result["a"].ReferencedPaths, []string{"b"})
}

func TestTfJsonModuleCalls(t *testing.T) {
	result := closures(t, map[string]string{
		"app/main.tf.json":       `{"module":{"network":{"source":"../shared/network"}}}`,
		"shared/network/main.tf": "output \"x\" { value = 1 }\n",
	}, "app")

	assertPaths(t, "referenced", result["app"].ReferencedPaths, []string{"shared/network"})
}

func TestNonLocalSourcesAreIgnored(t *testing.T) {
	result := closures(t, map[string]string{
		"app/main.tf": moduleCall("vpc", "terraform-aws-modules/vpc/aws") +
			moduleCall("remote", "git::https://example.com/repo.git//dir") +
			moduleCall("local", "../shared"),
		"shared/main.tf": "output \"x\" { value = 1 }\n",
	}, "app")

	assertPaths(t, "referenced", result["app"].ReferencedPaths, []string{"shared"})
}

func TestMissingTargetIsDangling(t *testing.T) {
	result := closures(t, map[string]string{
		"app/main.tf": moduleCall("gone", "../does/not/exist") + moduleCall("escape", "../../outside"),
	}, "app")

	assertPaths(t, "referenced", result["app"].ReferencedPaths, nil)
	assertPaths(t, "dangling", result["app"].DanglingPaths, []string{"../outside", "does/not/exist"})
}

func TestSameSubtreeReferenceIsTraversedButNotEmitted(t *testing.T) {
	result := closures(t, map[string]string{
		"app/main.tf":        moduleCall("nested", "./nested"),
		"app/nested/main.tf": moduleCall("outside", "../../shared"),
		"shared/main.tf":     "output \"x\" { value = 1 }\n",
	}, "app")

	assertPaths(t, "referenced", result["app"].ReferencedPaths, []string{"shared"})
}

func TestMultipleRootsShareParseMemo(t *testing.T) {
	result := closures(t, map[string]string{
		"a/main.tf":      moduleCall("common", "../common"),
		"b/main.tf":      moduleCall("common", "../common"),
		"common/main.tf": "output \"x\" { value = 1 }\n",
	}, "a", "b", "a")

	if len(result) != 2 {
		t.Fatalf("expected 2 unique closures, got %d", len(result))
	}
	assertPaths(t, "a", result["a"].ReferencedPaths, []string{"common"})
	assertPaths(t, "b", result["b"].ReferencedPaths, []string{"common"})
}

func TestNonTerraformDirectoryHasNoEdges(t *testing.T) {
	result := closures(t, map[string]string{
		"docs/README.md": "# docs\n",
	}, "docs")

	assertPaths(t, "referenced", result["docs"].ReferencedPaths, nil)
	assertPaths(t, "dangling", result["docs"].DanglingPaths, nil)
}
