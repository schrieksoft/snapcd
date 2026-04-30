variable "test_message" {
  description = "A test message string"
  type        = string
  default     = "Hello from SnapCD tests"
}

variable "resource_count" {
  description = "Number of test resources"
  type        = number
  default     = 1
}

variable "enable_feature" {
  description = "Boolean flag for testing"
  type        = bool
  default     = true
}
