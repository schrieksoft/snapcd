# Test file for output discovery

# Quoted output names
output "vpc_id" {
  description = "The ID of the VPC"
  value       = module.vpc.vpc_id
}

output "subnet_ids" {
  value     = module.vpc.subnet_ids
  sensitive = false
}

# Unquoted output names (also valid HCL)
output instance_ip {
  value = aws_instance.main.public_ip
}

output database_endpoint {
  description = "The database connection endpoint"
  value       = aws_db_instance.main.endpoint
  sensitive   = true
}

output empty_body {}
