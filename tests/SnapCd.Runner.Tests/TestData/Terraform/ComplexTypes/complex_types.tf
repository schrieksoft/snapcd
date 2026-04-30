variable "complex_object" {
  type = object({
    name    = string
    age     = number
    enabled = bool
  })
  description = "Complex object type"
}

variable "list_of_objects" {
  type = list(object({
    id   = string
    name = string
  }))
  default = []
}

variable "nested_map" {
  type = map(object({
    subnet_id = string
    cidr      = string
  }))
}

variable "with_validation" {
  type        = string
  description = "Variable with validation block"
  default     = "valid"

  validation {
    condition     = length(var.with_validation) > 0
    error_message = "Value cannot be empty"
  }
}
