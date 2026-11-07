# REST API ИС «Курьерская служба»

Базовый URL: `http://localhost:5080`
Swagger UI: `http://localhost:5080/swagger`
Формат данных: JSON (UTF-8)
Аутентификация: JWT Bearer, срок действия 120 минут.

---

## 1. Авторизация

### POST `/api/auth/login`

```json
{
  "login": "admin",
  "password": "admin123"
}
```

Ответ `200 OK`:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-11-07T05:00:00Z",
  "user": {
    "userId": 1,
    "login": "admin",
    "role": "Администратор",
    "roleCode": "Admin",
    "fio": "Ощепков Тимофей Сергеевич",
    "courierId": null
  }
}
```

Ошибки:

- `400` — пустой логин или пароль;
- `401` — неверные данные или заблокированная учётная запись.

### GET `/api/auth/me`

Возвращает данные текущего пользователя по токену.

---

## 2. Заказы

| Метод | Маршрут | Роли | Описание |
|---|---|---|---|
| GET | `/api/orders` | все | список с фильтрами `search`, `statusId`, `from`, `to`, `courierId` |
| GET | `/api/orders/{id}` | все* | заказ по идентификатору |
| GET | `/api/orders/{id}/history` | все* | история статусов |
| POST | `/api/orders` | Admin, Manager | создание заказа |
| PUT | `/api/orders/{id}` | Admin, Manager | изменение заказа |
| POST | `/api/orders/{id}/assign` | Admin, Manager | назначение курьера |
| POST | `/api/orders/{id}/status` | Admin, Manager, Courier* | смена статуса |
| DELETE | `/api/orders/{id}` | Admin, Manager | отмена заказа |

`*` Курьер видит и изменяет только заказы, назначенные на него.

### Создание заказа

```http
POST /api/orders
Authorization: Bearer <token>
Content-Type: application/json

{
  "clientId": 1,
  "senderAddress": "г. Ярославль, ул. Свободы, 10",
  "recipientFio": "Иванов Иван Иванович",
  "recipientPhone": "+7 900 123-45-67",
  "recipientAddress": "г. Ярославль, ул. Кирова, 5",
  "weight": 2.5,
  "zoneId": 1,
  "comment": "Первичная доставка",
  "paymentMethod": "Карта"
}
```

Ответ `201 Created`:

```json
{
  "id": 25,
  "nomer": "K-20261107-0001",
  "tariffId": 2,
  "price": 450.00
}
```

### Назначение курьера

```http
POST /api/orders/25/assign
Authorization: Bearer <token>
Content-Type: application/json

{ "courierId": 2 }
```

Ответ `200 OK` — обновлённый заказ. Если заказ завершён или отменён — `400`.

### Смена статуса

```http
POST /api/orders/25/status
Authorization: Bearer <token>
Content-Type: application/json

{
  "statusId": 5,
  "comment": "Доставлено лично получателю"
}
```

Статусы:

| id | Название |
|---|---|
| 1 | Новый |
| 2 | Принят |
| 3 | Назначен |
| 4 | В пути |
| 5 | Доставлен |
| 6 | Отменён |

---

## 3. Курьеры

| Метод | Маршрут | Роли |
|---|---|---|
| GET | `/api/couriers?search=` | все авторизованные |
| GET | `/api/couriers/{id}` | все авторизованные |
| GET | `/api/couriers/{id}/orders` | Admin, Manager, сам курьер |
| POST | `/api/couriers` | Admin, Manager |
| PUT | `/api/couriers/{id}` | Admin, Manager |
| DELETE | `/api/couriers/{id}` | Admin, Manager |

---

## 4. Клиенты

| Метод | Маршрут | Роли |
|---|---|---|
| GET | `/api/clients?search=` | Admin, Manager |
| GET | `/api/clients/{id}` | Admin, Manager |
| POST | `/api/clients` | Admin, Manager |
| PUT | `/api/clients/{id}` | Admin, Manager |
| DELETE | `/api/clients/{id}` | Admin, Manager |

---

## 5. Тарифы и зоны

| Метод | Маршрут | Роли |
|---|---|---|
| GET | `/api/tariffs?zoneId=` | все авторизованные |
| POST | `/api/tariffs` | Admin |
| PUT | `/api/tariffs/{id}` | Admin |
| DELETE | `/api/tariffs/{id}` | Admin |
| GET | `/api/zones` | все авторизованные |
| GET | `/api/statuses` | все авторизованные |

---

## 6. Отчёты

| Метод | Маршрут | Параметры |
|---|---|---|
| GET | `/api/reports/deliveries` | `from`, `to` |
| GET | `/api/reports/statuses` | `from`, `to` |
| GET | `/api/reports/courier-load` | — |
| GET | `/api/reports/revenue` | `from`, `to` |
| GET | `/api/reports/monthly` | `from`, `to` |

Доступ: Admin, Manager.

---

## 7. Панель управления

`GET /api/dashboard` — возвращает:

```json
{
  "ordersToday": 6,
  "ordersInWork": 5,
  "deliveredThisMonth": 2,
  "freeCouriers": 3,
  "revenueThisMonth": 4355.00,
  "clientsTotal": 10,
  "recentOrders": [ ... ]
}
```

---

## 8. Пользователи

| Метод | Маршрут | Роли |
|---|---|---|
| GET | `/api/users?search=` | Admin |
| GET | `/api/users/{id}` | Admin |
| POST | `/api/users` | Admin |
| PUT | `/api/users/{id}` | Admin |
| DELETE | `/api/users/{id}` | Admin |

Тело запроса:

```json
{
  "login": "newmanager",
  "password": "secret123",
  "roleId": 2,
  "fio": "Иванов Иван Иванович",
  "phone": "+7 900 000-00-00",
  "isActive": true
}
```

---

## 9. Коды ошибок

| Код | Причина |
|---|---|
| 200 | успешно |
| 201 | создано |
| 204 | удалено / отменено |
| 400 | ошибка валидации, не найден тариф, нельзя назначить курьера |
| 401 | нет токена или неверные данные |
| 403 | недостаточно прав |
| 404 | объект не найден |
| 409 | дубликат уникального поля |
| 500 | внутренняя ошибка |

Формат ошибки:

```json
{
  "error": "Для указанной зоны и веса не найден тариф.",
  "code": "VALIDATION_ERROR",
  "detail": null
}
```
