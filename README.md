# Nord Shop — Backend API

REST API интернет-магазина на ASP.NET Core. Отвечает за тестовую авторизацию, каталог, оформление заказов и доступ к данным PostgreSQL.

**Production API:** <https://api.vitaliy-armss.ru/nord-shop/api>  
**Health check:** <https://api.vitaliy-armss.ru/nord-shop/health>

## Стек

- C# / ASP.NET Core (.NET 10)
- Entity Framework Core
- PostgreSQL / Npgsql
- JWT Bearer authentication
- Docker / Docker Compose
- Nginx reverse proxy

## Основные маршруты

| Метод | Маршрут внутри API | Назначение |
|---|---|---|
| `GET` | `/health` | Проверка приложения и БД |
| `GET` | `/api/products` | Получить каталог |
| `GET` | `/api/products/{id}` | Получить товар |
| `POST` | `/api/auth/login` | Войти по тестовому аккаунту |
| `GET` | `/api/auth/me` | Получить данные текущего пользователя |
| `GET` | `/api/orders/mine` | История заказов пользователя |
| `POST` | `/api/orders` | Создать заказ |

Внешний префикс `/nord-shop/` обрабатывается Nginx. Например, `https://api.vitaliy-armss.ru/nord-shop/api/products` проксируется в backend как `/api/products`.

## Тестовые аккаунты

- Покупатель: `demo` / `demo123`
- Менеджер: `manager` / `manager123`

Создание аккаунтов и реальная оплата не предусмотрены. Эти учётные данные публичные и предназначены только для демонстрации.

## Развёртывание на VPS

1. Распакуй проект, создай `.env` на основе `.env.example`.
2. Задай `POSTGRES_NETWORK` по Docker-сети уже работающего контейнера `messenger-postgres`.
3. Создай отдельную базу `nord-shop` и роль `nord-shop_app` в существующем PostgreSQL.
4. Импортируй `database/nord-shop-db-dump.sql` в новую пустую базу.
5. Собери и запусти API:

```bash
sudo docker compose config --quiet
sudo docker compose up -d --build
sudo docker compose logs --tail=100 -f nord-shop-api
```

API публикуется только на `127.0.0.1:5003`. Nginx направляет внешний трафик `/nord-shop/` на этот порт. Перед импортом дампа убедись, что база пустая: это файл начальной структуры и тестовых данных, а не обновление существующей базы.

## Конфигурация и безопасность

- Храни `POSTGRES_PASSWORD` и `JWT_KEY` в серверном `.env` и исключай его из Git.
- Не открывай порт `5003` напрямую в интернет; внешний доступ должен идти через HTTPS Nginx.
- Разрешённый hostname задаётся через `AllowedHosts`.
- Реальные платежи, публичная регистрация и пользовательское управление товарами в этом демо не реализованы.
