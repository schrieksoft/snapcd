terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

variable "from_b_other" {
  type = string
}

resource "random_pet" "f" {
  prefix = var.from_b_other
}

output "f_id" {
  value = random_pet.f.id
}
