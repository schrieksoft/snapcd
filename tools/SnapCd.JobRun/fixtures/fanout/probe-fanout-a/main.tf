terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

# keepers forces replacement, so changing it makes a_id unknown. That unknown is what reaches
# b, c and d.
resource "random_pet" "a" {
  keepers = {
    generation = "2"
  }
}

output "a_id" {
  value = random_pet.a.id
}
