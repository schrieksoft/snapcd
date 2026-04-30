output "test_id" {
  description = "Generated UUID for testing"
  value       = random_uuid.test_id.result
}

output "test_message" {
  description = "Echo of the input message"
  value       = var.test_message
}

output "resource_count" {
  description = "Echo of the resource count"
  value       = var.resource_count
}

output "enabled" {
  description = "Echo of the feature flag"
  value       = var.enable_feature
}
