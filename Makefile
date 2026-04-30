network-up:
	-docker network create snapcd-localdev-network

network-down:
	-docker network rm snapcd-localdev-network
	
containers-up:
	docker compose -f docker-compose.yml up -d  --remove-orphans

containers-down:	
	docker compose -f docker-compose.yml down
	
up: network-up containers-up

down: containers-down network-down

logs:
	docker compose -f docker-compose.yml logs sql-server-db

exec:
	docker compose -f docker-compose.yml exec sql-server-db

restart:
	docker compose -f docker-compose.yml restart --no-deps sql-server-db


