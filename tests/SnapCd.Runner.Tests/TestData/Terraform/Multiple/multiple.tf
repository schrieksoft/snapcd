# Multiple variables with different types and attributes

variable "region" {
  type        = string
  description = "AWS region"
  default     = "us-east-1"
}

variable "instance_count" {
  type        = number
  description = "Number of instances"
  default     = 3
}

variable "enable_monitoring" {
  type    = bool
  default = true
}

variable "tags" {
  type = map(string)
  default = {
    Environment = "production"
    Team        = "platform"
  }
}

variable "availability_zones" {
  type        = list(string)
  description = "List of AZs"
  default     = ["us-east-1a", "us-east-1b", "us-east-1c"]
}

variable "api_key" {
  type        = string
  description = "API key for service"
  sensitive   = true
}

variable "optional_value" {
  type     = string
  nullable = true
  default  = null
}

variable "required_value" {
  type        = string
  description = "This is required because it has no default"
}