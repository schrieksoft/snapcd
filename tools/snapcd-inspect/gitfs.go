// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

package main

import (
	"bytes"
	"fmt"
	"io"
	"io/fs"
	"os/exec"
	"strings"
	"time"
)

// gitFS is a read-only io/fs.FS over the tree of one commit in a (bare) git repository, so terraform files can
// be parsed without materializing a working tree.
type gitFS struct {
	repoDir string
	commit  string
}

func (g gitFS) git(args ...string) ([]byte, error) {
	cmd := exec.Command("git", append([]string{"-C", g.repoDir}, args...)...)
	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		return nil, fmt.Errorf("git %s: %w: %s", strings.Join(args, " "), err, stderr.String())
	}
	return stdout.Bytes(), nil
}

func (g gitFS) treeRef(name string) string {
	if name == "." {
		return g.commit + "^{tree}"
	}
	return g.commit + ":" + name
}

func (g gitFS) Open(name string) (fs.File, error) {
	if !fs.ValidPath(name) {
		return nil, &fs.PathError{Op: "open", Path: name, Err: fs.ErrInvalid}
	}
	contents, err := g.git("cat-file", "blob", g.commit+":"+name)
	if err != nil {
		return nil, &fs.PathError{Op: "open", Path: name, Err: fs.ErrNotExist}
	}
	return &memFile{name: name, reader: bytes.NewReader(contents), size: int64(len(contents))}, nil
}

func (g gitFS) ReadFile(name string) ([]byte, error) {
	if !fs.ValidPath(name) {
		return nil, &fs.PathError{Op: "readfile", Path: name, Err: fs.ErrInvalid}
	}
	contents, err := g.git("cat-file", "blob", g.commit+":"+name)
	if err != nil {
		return nil, &fs.PathError{Op: "readfile", Path: name, Err: fs.ErrNotExist}
	}
	return contents, nil
}

func (g gitFS) ReadDir(name string) ([]fs.DirEntry, error) {
	if !fs.ValidPath(name) {
		return nil, &fs.PathError{Op: "readdir", Path: name, Err: fs.ErrInvalid}
	}
	output, err := g.git("ls-tree", g.treeRef(name))
	if err != nil {
		return nil, &fs.PathError{Op: "readdir", Path: name, Err: fs.ErrNotExist}
	}

	var entries []fs.DirEntry
	for _, line := range strings.Split(strings.TrimSuffix(string(output), "\n"), "\n") {
		if line == "" {
			continue
		}
		tabIndex := strings.IndexByte(line, '\t')
		if tabIndex < 0 {
			continue
		}
		meta := strings.Fields(line[:tabIndex])
		if len(meta) < 3 {
			continue
		}
		entries = append(entries, dirEntry{name: line[tabIndex+1:], isDir: meta[1] == "tree"})
	}
	return entries, nil
}

type memFile struct {
	name   string
	reader *bytes.Reader
	size   int64
}

func (f *memFile) Stat() (fs.FileInfo, error) { return fileInfo{name: f.name, size: f.size}, nil }
func (f *memFile) Read(p []byte) (int, error) { return f.reader.Read(p) }
func (f *memFile) Close() error               { return nil }

var _ io.Seeker = (*memFile)(nil)

func (f *memFile) Seek(offset int64, whence int) (int64, error) { return f.reader.Seek(offset, whence) }

type dirEntry struct {
	name  string
	isDir bool
}

func (e dirEntry) Name() string               { return e.name }
func (e dirEntry) IsDir() bool                { return e.isDir }
func (e dirEntry) Type() fs.FileMode          { return fileInfo{name: e.name, dir: e.isDir}.Mode().Type() }
func (e dirEntry) Info() (fs.FileInfo, error) { return fileInfo{name: e.name, dir: e.isDir}, nil }

type fileInfo struct {
	name string
	size int64
	dir  bool
}

func (i fileInfo) Name() string { return i.name }
func (i fileInfo) Size() int64  { return i.size }
func (i fileInfo) Mode() fs.FileMode {
	if i.dir {
		return fs.ModeDir | 0o555
	}
	return 0o444
}
func (i fileInfo) ModTime() time.Time { return time.Time{} }
func (i fileInfo) IsDir() bool        { return i.dir }
func (i fileInfo) Sys() any           { return nil }
