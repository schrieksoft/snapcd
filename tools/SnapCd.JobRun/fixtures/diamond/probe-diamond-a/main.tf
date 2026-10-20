terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

# keepers forces replacement, so changing it makes pet_id unknown and that
# unknown is what has to reach d through both b and c.
resource "random_pet" "a" {
  keepers = {
    generation = "2"
  }
}

output "a_id" {
  value = random_pet.a.id
}

# Deliberately not keyed on the generation above, so this output does not change when a_id does.
# e reads this, which is how the fixture proves an uninvolved Module stays out of a wave.
resource "random_pet" "a_stable" {
  keepers = {
    fixed = "constant"
  }
}

output "a_other" {
  value = random_pet.a_stable.id
}
