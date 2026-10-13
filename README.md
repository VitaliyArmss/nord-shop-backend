# nord-shop backend — ASP.NET Core + PostgreSQL

Самостоятельный backend-проект для VPS: ASP.NET Core на .NET 10 LTS, Docker Compose, JWT и PostgreSQL. Архив содержит SQL-файл начальной схемы и демонстрационных данных, а также фрагмент конфигурации Nginx.

- Публичный API: `https://api.vitaliy-armss.ru/nord-shop/api`
- Health endpoint: `https://api.vitaliy-armss.ru/nord-shop/health`
- Внутренний порт на VPS: `127.0.0.1:5003`
- PostgreSQL: существующий контейнер `messenger-postgres`; новый PostgreSQL контейнер не создаётся.
- База данных: `nord-shop`; роль БД: `nord-shop_app`.

## 1. Передать backend на VPS

На своём компьютере: `scp nord-shop-backend-production.zip YOUR_USER@YOUR_VPS:/tmp/nord-shop-backend-production.zip`. На VPS выполни:

```bash
sudo mkdir -p /opt/nord-shop
sudo chown "$USER":"$USER" /opt/nord-shop
unzip /tmp/nord-shop-backend-production.zip -d /opt/nord-shop
cd /opt/nord-shop
cp .env.example .env
nano .env
```

Найди Docker-сеть, в которой работает PostgreSQL:

```bash
sudo docker inspect -f '{{range $name, $_ := .NetworkSettings.Networks}}{{println $name}}{{end}}' messenger-postgres
```

Укажи одну из подходящих сетей в `POSTGRES_NETWORK` в `.env`. Оставь `POSTGRES_DB=nord-shop`, `POSTGRES_USER=nord-shop_app` и сгенерируй пароли. Например, `openssl rand -hex 24` для `POSTGRES_PASSWORD` и `openssl rand -base64 48` для `JWT_KEY`. Сгенерированные значения сохрани: они понадобятся ниже. Не коммить файл `.env` в Git.

## 2. Создать отдельную роль и базу в существующем PostgreSQL

Подключись к контейнеру действующим PostgreSQL-администратором. Команда ниже предполагает, что имя администратора — `postgres`; если оно отличается, замени его на актуальное:

```bash
sudo docker exec -it messenger-postgres psql -U postgres -d postgres
```

В консоли `psql` создай отдельную роль и базу. Подставь фактический пароль из `.env` вместо значения-заполнителя. Из-за дефиса имена в SQL заключены в двойные кавычки:

```sql
CREATE ROLE "nord-shop_app" LOGIN PASSWORD 'ВСТАВЬ_СЮДА_POSTGRES_PASSWORD_ИЗ_ENV';
CREATE DATABASE "nord-shop" OWNER "nord-shop_app";
\q
```

Эти команды выполняются один раз. Если роль или база уже существуют, сначала проверь их и права — не создавай дубликаты.

## 3. Импортировать начальный SQL dump

Выполни на VPS из `/opt/nord-shop`. Импортировать нужно в только что созданную пустую базу:

```bash
cd /opt/nord-shop
set -a
. ./.env
set +a
sudo docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" -i messenger-postgres \
  psql -h 127.0.0.1 -v ON_ERROR_STOP=1 \
  -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  < database/nord-shop-db-dump.sql
```

SQL dump создаёт таблицы, индексы и ограничения и добавляет 8 демотоваров и двух тестовых пользователей. Это начальная загрузка схемы, а не копия старых данных с сервера. Не импортируй этот файл повторно в непустую базу.

## 4. Собрать и запустить API на порту 5003

```bash
cd /opt/nord-shop
sudo docker compose config --quiet
sudo docker compose up -d --build
sudo docker compose ps
sudo docker compose logs --tail=100 -f nord-shop-api
```

Проверь API непосредственно на VPS:

```bash
curl -i http://127.0.0.1:5003/health
curl -s http://127.0.0.1:5003/api/products
```

Порт доступен только через loopback, поэтому внешние соединения должны проходить через Nginx. В Production Swagger не включён.

## 5. Настроить Nginx для `/nord-shop`

Найди существующий HTTPS `server`-блок для `api.vitaliy-armss.ru` и добавь внутрь него содержимое `deploy/nginx-nord-shop-location.conf`. Не заменяй весь конфигурационный файл и не удаляй существующие маршруты приложений на портах `5000`, `5001` и `5002`.

Главная деталь — завершающий слеш в `proxy_pass http://127.0.0.1:5003/;`: Nginx удалит внешний префикс `/nord-shop/` и передаст `/api/products` в ASP.NET Core. После редактирования проверь конфигурацию и перечитай её:

```bash
sudo nginx -t
sudo systemctl reload nginx
curl -i https://api.vitaliy-armss.ru/nord-shop/health
curl -s https://api.vitaliy-armss.ru/nord-shop/api/products
```

Этот upstream подходит, если Nginx установлен непосредственно на хосте VPS. Если Nginx запущен в Docker, подключи контейнер Nginx к той же Docker-сети и используй `proxy_pass http://nord-shop-api:5003/;`.

## Тестовые аккаунты

- Покупатель: `demo` / `demo123`
- Менеджер: `manager` / `manager123`

Регистрации и настоящей оплаты нет. Ограничение количества попыток входа включено. API не создаёт и не изменяет схему автоматически; обновления базы следует проводить контролируемыми SQL-миграциями.

## Резервная копия базы после развёртывания

После создания базы и настройки `.env` на VPS можно сделать отдельный backup текущей базы:

```bash
cd /opt/nord-shop
set -a
. ./.env
set +a
sudo docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" messenger-postgres \
  pg_dump -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  --format=custom --no-owner > /opt/nord-shop/nord-shop-backup.dump
chmod 600 /opt/nord-shop/nord-shop-backup.dump
```

Скачать файл на свой компьютер: `scp YOUR_USER@YOUR_VPS:/opt/nord-shop/nord-shop-backup.dump .`. Храни резервные копии за пределами контейнера PostgreSQL и проверяй восстановление на отдельной тестовой базе. Не используй `docker compose down -v` для существующего контейнера `messenger-postgres`.
