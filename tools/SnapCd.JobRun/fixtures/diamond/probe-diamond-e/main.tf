terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

variable "from_a_other" {
  type = string
}

resource "random_pet" "e" {
  prefix = var.from_a_other
}

output "e_id" {
  value = random_pet.e.id
}
