// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

// snapcd-inspect resolves, for each requested root directory, the transitive closure of locally-referenced
// terraform directories at one commit of a (bare) git repository. Only literal local module sources ("./x",
// "../y") constitute edges — terraform requires module source arguments to be literal strings, which is what
// makes this discovery exact. Registry and remote sources are ignored; references escaping the repository root
// or pointing at missing directories are reported as dangling.
//
// Usage: snapcd-inspect --repo <path-to-clone> --commit <sha> --roots <dir>[,<dir>...]
//
// Output (stdout): {"closures":[{"rootPath":"...","referencedPaths":[...],"danglingPaths":[...]}]}
package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"os"
	"path"
	"sort"
	"strings"

	"github.com/hashicorp/terraform-config-inspect/tfconfig"
)

type Closure struct {
	RootPath        string   `json:"rootPath"`
	ReferencedPaths []string `json:"referencedPaths"`
	DanglingPaths   []string `json:"danglingPaths"`
}

type Result struct {
	Closures []Closure `json:"closures"`
}

type directRefs struct {
	refs     []string
	dangling []string
}

type inspector struct {
	fs   tfconfig.FS
	memo map[string]directRefs
}

func normalize(p string) string {
	trimmed := strings.Trim(strings.TrimSpace(p), "/")
	if trimmed == "" {
		return "."
	}
	return path.Clean(trimmed)
}

// direct returns the directory's immediate local module reference targets (repo-root-relative) and any dangling
// references. A directory that does not exist or holds no terraform files simply has no edges.
func (i *inspector) direct(dir string) directRefs {
	if cached, ok := i.memo[dir]; ok {
		return cached
	}

	result := directRefs{}
	module, _ := tfconfig.LoadModuleFromFilesystem(i.fs, dir)
	if module != nil {
		for _, call := range module.ModuleCalls {
			source := call.Source
			if !strings.HasPrefix(source, "./") && !strings.HasPrefix(source, "../") {
				continue
			}

			target := path.Join(dir, source)
			if target == ".." || strings.HasPrefix(target, "../") {
				result.dangling = append(result.dangling, target)
				continue
			}
			if _, err := i.fs.ReadDir(target); err != nil {
				result.dangling = append(result.dangling, target)
				continue
			}
			result.refs = append(result.refs, target)
		}
	}

	sortUnique(&result.refs)
	sortUnique(&result.dangling)
	i.memo[dir] = result
	return result
}

// closure walks the reference graph breadth-first from the root, cycle-safe. Referenced paths inside the root's
// own subtree are traversed (their outward references still count) but excluded from the output — the root's
// tree hash already covers them.
func (i *inspector) closure(root string) Closure {
	visited := map[string]bool{root: true}
	queue := []string{root}
	referenced := map[string]bool{}
	dangling := map[string]bool{}

	for len(queue) > 0 {
		current := queue[0]
		queue = queue[1:]

		refs := i.direct(current)
		for _, d := range refs.dangling {
			dangling[d] = true
		}
		for _, ref := range refs.refs {
			if !withinSubtree(root, ref) {
				referenced[ref] = true
			}
			if !visited[ref] {
				visited[ref] = true
				queue = append(queue, ref)
			}
		}
	}

	return Closure{
		RootPath:        root,
		ReferencedPaths: sortedKeys(referenced),
		DanglingPaths:   sortedKeys(dangling),
	}
}

func withinSubtree(root, candidate string) bool {
	if root == "." {
		return true
	}
	return candidate == root || strings.HasPrefix(candidate, root+"/")
}

func sortUnique(values *[]string) {
	seen := map[string]bool{}
	var unique []string
	for _, v := range *values {
		if !seen[v] {
			seen[v] = true
			unique = append(unique, v)
		}
	}
	sort.Strings(unique)
	*values = unique
}

func sortedKeys(set map[string]bool) []string {
	keys := make([]string, 0, len(set))
	for key := range set {
		keys = append(keys, key)
	}
	sort.Strings(keys)
	return keys
}

func run(repoDir, commit string, roots []string) (Result, error) {
	inspect := &inspector{
		fs:   tfconfig.WrapFS(gitFS{repoDir: repoDir, commit: commit}),
		memo: map[string]directRefs{},
	}

	result := Result{Closures: []Closure{}}
	seen := map[string]bool{}
	for _, root := range roots {
		root = normalize(root)
		if seen[root] {
			continue
		}
		seen[root] = true
		result.Closures = append(result.Closures, inspect.closure(root))
	}
	return result, nil
}

func main() {
	repoDir := flag.String("repo", "", "path to the (bare) git repository")
	commit := flag.String("commit", "", "commit SHA to inspect")
	roots := flag.String("roots", "", "comma-separated repo-root-relative directories to resolve closures for")
	flag.Parse()

	if *repoDir == "" || *commit == "" || *roots == "" {
		fmt.Fprintln(os.Stderr, "usage: snapcd-inspect --repo <path> --commit <sha> --roots <dir>[,<dir>...]")
		os.Exit(2)
	}

	result, err := run(*repoDir, *commit, strings.Split(*roots, ","))
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	encoder := json.NewEncoder(os.Stdout)
	if err := encoder.Encode(result); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
