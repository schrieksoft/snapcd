terraform {
  required_providers {
    null = {
      source  = "hashicorp/null"
      version = "3.2.4"
    }
  }
}

variable "from_a" {
  type = string
}

resource "null_resource" "d" {
  triggers = {
    upstream = var.from_a
  }
}

output "d_id" {
  value = "settled-marmot-brave-moth"
}
