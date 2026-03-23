# FundingMonitor - Docker

## 📋 Компоненты

| Сервис         | Порт | Описание                    |
| -------------- | ---- | --------------------------- |
| **PostgreSQL** | 5432 | База данных                 |
| **Redis**      | 6379 | Персистентная очередь задач |

---

## 🚀 Быстрый старт

### **1. Запустить все сервисы:**

```bash
docker-compose up -d
```

### **2. Проверить статус:**

```bash
docker-compose ps
```

**Ожидаемый результат:**

```
NAME                      STATUS                    PORTS
funding_monitor_db        Up (healthy)              0.0.0.0:5432->5432/tcp
funding_monitor_redis     Up (healthy)              0.0.0.0:6379->6379/tcp
```

---

## 🔧 Управление

### **Остановить все сервисы:**

```bash
docker-compose down
```

### **Остановить и удалить данные:**

```bash
docker-compose down -v
```

### **Перезапустить сервис:**

```bash
docker-compose restart postgres
docker-compose restart redis
```

---

## 📊 Логи

### **Все логи:**

```bash
docker-compose logs -f
```

### **Только PostgreSQL:**

```bash
docker-compose logs -f postgres
```

### **Только Redis:**

```bash
docker-compose logs -f redis
```

---

## 🗄️ База данных

### **Подключиться к PostgreSQL:**

```bash
docker exec -it funding_monitor_db psql -U postgres -d funding_monitor
```

### **Проверить таблицы:**

```sql
\dt
```

### **Проверить миграции:**

```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
```

---

## 🔴 Redis

### **Подключиться к Redis:**

```bash
docker exec -it funding_monitor_redis redis-cli
```

### **Проверить очередь:**

```bash
LLEN funding_monitor:history_queue
```

### **Посмотреть задачи:**

```bash
LRANGE funding_monitor:history_queue 0 -1
```

### **Очистить очередь:**

```bash
DEL funding_monitor:history_queue
```

---

## 🐛 Отладка

### **Проверить health сервисов:**

```bash
docker-compose ps
```

### **Если PostgreSQL не запускается:**

```bash
# Проверить логи
docker-compose logs postgres

# Удалить volume и запустить заново
docker-compose down -v
docker-compose up -d postgres
```

### **Если Redis не запускается:**

```bash
# Проверить логи
docker-compose logs redis

# Перезапустить
docker-compose restart redis
```

---

## 📁 Тома

| Том             | Назначение         |
| --------------- | ------------------ |
| `postgres_data` | Данные PostgreSQL  |
| `redis_data`    | Данные Redis (AOF) |

---

## 🔐 Безопасность

**⚠️ Для production измените:**

1. Пароль PostgreSQL в `docker-compose.yml`
2. Ограничьте доступ к портам (firewall)
3. Используйте secrets для чувствительных данных

---

## 📊 Мониторинг

### **Использование ресурсов:**

```bash
docker stats funding_monitor_db funding_monitor_redis
```

### **Размер volumes:**

```bash
docker system df -v
```

---

## 🔄 Обновление

### **Обновить образы:**

```bash
docker-compose pull
docker-compose up -d
```

---

## 📝 Примечания

- **Данные:** Сохраняются в volumes (не удаляются при `down`)
- **Порты:** 5432 (PostgreSQL), 6379 (Redis)
- **Сеть:** Изолированная `funding_monitor`
