// Single line comment with //
# Single line comment with #

/*
 * Multi-line comment
 * with multiple lines
 */

variable "with_comments" {
  type = string # inline comment
  // another comment
  description = "Variable with comments" // trailing comment
  default     = "value" # end of line
}

variable "no_comments" {
  type        = string
  description = "Clean variable"
  default     = "clean"
}
