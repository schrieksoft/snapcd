terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

variable "from_a" {
  type = string
}

resource "random_pet" "c" {
  prefix = var.from_a
}

output "c_id" {
  value = random_pet.c.id
}
