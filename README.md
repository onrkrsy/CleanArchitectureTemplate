# 🏗️ Clean Architecture Template

.NET 8 ile Clean Architecture prensiplerine uygun olarak geliştirilmiş kapsamlı Web API template'i. Modern web geliştirme ihtiyaçlarına yönelik hazır altyapı bileşenleri içerir.

## 🎯 Proje Özellikleri

### 🔐 Kimlik Doğrulama & Yetkilendirme
- **JWT Authentication** - Token tabanlı güvenli kimlik doğrulama
- **Role-based Authorization** - Rol tabanlı yetki yönetimi
- **Password Security** - PBKDF2 + SHA256 şifreleme (100,000 iterasyon)

### 🚀 Performans & Ölçeklenebilirlik
- **Multi-Database Desteği** - SQL Server, PostgreSQL, InMemory
- **Dapper Integration** - Yüksek performanslı raw SQL sorguları
- **Advanced Caching** - InMemory, Redis, Hybrid cache stratejileri
- **Connection Pooling** - Otomatik veritabanı bağlantı yönetimi

### 🛡️ Cross-Cutting Concerns
- **Global Exception Handling** - Merkezi hata yönetimi
- **Comprehensive Logging** - Serilog ile yapılandırılabilir log sistemi
- **Transaction Management** - Declarative ve programmatic transaction desteği
- **Attribute-Based Programming** - Cache, Log, Audit attribute'leri

### 🔧 Geliştirici Deneyimi
- **Code Generator** - Otomatik CRUD kod üretimi
- **API Documentation** - Swagger/OpenAPI entegrasyonu
- **Extension Methods** - Kullanışlı yardımcı metodlar
- **Result Pattern** - Tutarlı API response yapısı

## 📁 Proje Yapısı

```
ca-template/
├── src/
│   ├── API/                    # 🌐 Presentation Layer
│   │   ├── Controllers/        # HTTP endpoints
│   │   ├── Middlewares/        # Request/Response işleme
│   │   └── Program.cs          # Uygulama başlangıcı
│   │
│   ├── Application/            # 📋 Application Layer  
│   │   ├── DTOs/              # Data Transfer Objects
│   │   ├── Interfaces/        # Service contract'ları
│   │   └── Mappings/          # AutoMapper profilleri
│   │
│   ├── Domain/                 # 🏛️ Domain Layer
│   │   ├── Entities/          # İş nesneleri (User, Role)
│   │   └── Enums/             # Domain enum'ları
│   │
│   ├── Infrastructure/         # 🔧 Infrastructure Layer
│   │   ├── Data/              # DbContext, Migrations
│   │   ├── Repositories/      # Veri erişim implementasyonları
│   │   └── Services/          # External service'ler
│   │
│   └── Common/                 # 🔄 Shared Components
│       ├── Interfaces/        # Generic interface'ler
│       ├── Repositories/      # Base repository'ler
│       ├── Services/          # Ortak service'ler
│       ├── Extensions/        # Extension metodları
│       ├── Models/            # Ortak modeller
│       └── Utilities/         # Yardımcı sınıflar
│
├── tools/
│   └── CodeGenerator/          # 🎯 Otomatik kod üretici
│       ├── Templates/         # Kod şablonları
│       ├── Services/          # Generator service'leri
│       └── README.md          # Generator kullanım kılavuzu
│
└── README.md                   # 📖 Bu dosya
```

## 🚀 Hızlı Başlangıç

### 📋 Gereksinimler
- **.NET 8 SDK** (8.0.0 veya üzeri)
- **IDE** (Visual Studio 2022, VS Code, JetBrains Rider)
- **Git** (kod yönetimi için)

### ⚡ Kurulum Adımları

1. **Projeyi klonlayın**
```bash
git clone <repository-url>
cd ca-template
```

2. **Bağımlılıkları yükleyin**
```bash
dotnet restore
```

3. **Veritabanını hazırlayın**
```bash
# Entity Framework migration'ları çalıştır
cd src/Infrastructure
dotnet ef database update
```

4. **Uygulamayı çalıştırın**
```bash
cd src/API
dotnet run
```

5. **API'yi test edin**
```bash
# Swagger UI
http://localhost:5000/swagger

# API base URL
http://localhost:5000/api/v1
```

## 👥 Hazır Test Kullanıcıları

Uygulama ilk çalıştırıldığında otomatik olarak test kullanıcıları oluşturulur:

| Email | Şifre | Rol | Yetkiler |
|-------|-------|-----|----------|
| `admin@example.com` | `admin123` | **Admin** | ✅ Tüm işlemler (CRUD) |
| `jane.smith@example.com` | `123456` | **Manager** | ✅ Okuma, Güncelleme ❌ Oluşturma, Silme |
| `john.doe@example.com` | `123456` | **User** | ✅ Sadece okuma |

## 🔐 Kimlik Doğrulama Kullanımı

### 🔑 Giriş Yapma

```bash
POST /api/v1/users/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "admin123"
}
```

**Başarılı Yanıt:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 3,
    "firstName": "Admin",
    "lastName": "User", 
    "email": "admin@example.com",
    "role": "Admin"
  }
}
```

### 🛡️ Token Kullanımı

Korumalı endpoint'lere erişmek için token'ı Authorization header'ında gönderin:

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 📊 API Endpoint'leri

### 🌐 Kullanıcı Yönetimi Endpoint'leri

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| `POST` | `/api/v1/users/login` | 🔓 Herkese açık | Kullanıcı girişi |
| `GET` | `/api/v1/users` | 🔒 Giriş gerekli | Tüm kullanıcıları listele |
| `GET` | `/api/v1/users/active` | 🔒 Giriş gerekli | Aktif kullanıcıları listele |
| `GET` | `/api/v1/users/{id}` | 🔓 Herkese açık | ID'ye göre kullanıcı getir |
| `GET` | `/api/v1/users/by-email/{email}` | 🔓 Herkese açık | Email'e göre kullanıcı getir |
| `POST` | `/api/v1/users` | 🔴 Sadece Admin | Yeni kullanıcı oluştur |
| `PUT` | `/api/v1/users/{id}` | 🟡 Admin, Manager | Kullanıcı bilgilerini güncelle |
| `DELETE` | `/api/v1/users/{id}` | 🔴 Sadece Admin | Kullanıcı sil |

### 📝 Kullanım Örnekleri

#### 1️⃣ Admin ile Yeni Kullanıcı Oluşturma

```bash
# Adım 1: Admin girişi
POST /api/v1/users/login
{
  "email": "admin@example.com",
  "password": "admin123"
}

# Adım 2: Token ile yeni kullanıcı oluştur
POST /api/v1/users
Authorization: Bearer <admin-token>
{
  "firstName": "Yeni",
  "lastName": "Kullanıcı",
  "email": "yeni@example.com",
  "phoneNumber": "+905551234567",
  "password": "test123",
  "roleId": 2
}
```

#### 2️⃣ Manager ile Kullanıcı Güncelleme

```bash
# Adım 1: Manager girişi
POST /api/v1/users/login
{
  "email": "jane.smith@example.com",
  "password": "123456"
}

# Adım 2: Kullanıcı bilgileri güncelle
PUT /api/v1/users/1
Authorization: Bearer <manager-token>
{
  "firstName": "Güncellenmiş",
  "lastName": "İsim",
  "email": "guncellenmis@example.com",
  "phoneNumber": "+905559876543",
  "isActive": true
}
```

## 🔧 Konfigürasyon

### ⚙️ Temel Ayarlar (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration2024!",
    "Issuer": "CleanArchitectureTemplate",
    "Audience": "CleanArchitectureTemplate", 
    "ExpiryMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "InMemory",
    "SqlServer": "Server=localhost;Database=CleanArchDb;Trusted_Connection=true;",
    "PostgreSQL": "Host=localhost;Database=CleanArchDb;Username=postgres;Password=password;Port=5432;",
    "Redis": "localhost:6379"
  },
  "DatabaseProvider": "InMemory",
  "CacheProvider": "InMemory"
}
```

### 🗄️ Veritabanı Yapılandırması

#### Development (Geliştirme)
```json
{
  "DatabaseProvider": "InMemory",
  "ConnectionStrings": {
    "DefaultConnection": "InMemory"
  }
}
```

#### Production (Üretim)
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CleanArchDb;Trusted_Connection=true;"
  }
}
```

## 🚀 Gelişmiş Özellikler

### 💾 Önbellek (Cache) Sistemi

#### Temel Kullanım
```csharp
// Otomatik önbellekleme
[Cache("user_list", ExpirationMinutes = 10)]
public async Task<List<UserDto>> GetUsersAsync()
{
    return await _userRepository.GetAllAsync();
}

// Parametreli önbellekleme
[Cache("user", KeyParameters = new[] { "id" }, ExpirationMinutes = 5)]
public async Task<UserDto> GetUserByIdAsync(int id)
{
    return await _userRepository.GetByIdAsync(id);
}
```

#### Önbellek Temizleme
```csharp
// Otomatik önbellek temizleme
[CacheEvict(KeyPatterns = new[] { "user_*", "user_list" })]
public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
{
    return await _userRepository.CreateAsync(dto);
}
```

### 📋 Loglama Sistemi

#### Otomatik Loglama
```csharp
// Request/Response logları
[Log(LogLevel.Information, LogParameters = true, LogExecutionTime = true)]
public async Task<UserDto> GetUserAsync(int id)
{
    return await _userRepository.GetByIdAsync(id);
}

// Hassas veri gizleme
[Log(SensitiveParameters = new[] { "password" })]
public async Task<LoginResult> LoginAsync(LoginDto dto)
{
    return await _authService.LoginAsync(dto);
}
```

### 🔄 İşlem (Transaction) Yönetimi

#### Otomatik İşlem Yürütme
```csharp
[Transactional(IsolationLevel.ReadCommitted, Timeout = 30)]
public async Task<UserDto> CreateUserWithProfileAsync(CreateUserDto dto)
{
    var user = await _userService.CreateAsync(dto);
    var profile = await _profileService.CreateAsync(user.Id);
    return user;
}
```

## 🛠️ Kod Üretici (Code Generator)

### 🎯 Temel Kullanım

```bash
# Global tool kurulumu (tek seferlik)
cd tools/CodeGenerator
dotnet pack
dotnet tool install --global --add-source ./bin/Release CleanArchitecture.CodeGenerator

# Herhangi bir projede kullanım
ca-generator --entity Product
ca-generator --entity Order --overwrite
ca-generator --entity Category --dry-run
```

### 📁 Üretilen Dosyalar

- **DTOs** - `CreateDto`, `UpdateDto`, `ResponseDto`
- **Repository** - Interface ve Implementation
- **Service** - Interface ve Implementation
- **Controller** - REST API endpoints
- **AutoMapper** - Mapping profilleri

### 🎨 Özel Endpoint'ler

```bash
# Özel endpoint'lerle kod üretimi
ca-generator --entity Product --custom-actions GetActive,Activate
ca-generator --entity Order --actions-only
```

## 📊 Performans Özellikleri

### ⚡ Repository Pattern

```csharp
// Performans için tracking kontrolü
var users = await _repository.GetAll().ToListAsync(); // No-tracking (hızlı)
var user = await _repository.GetByIdWithTracking(id); // Tracking (güncelleme için)

// Veritabanı seviyesinde filtreleme
var activeUsers = await _repository.Where(u => u.IsActive).ToListAsync();

// Bulk işlemler
await _repository.AddRangeAsync(users);
await _repository.UpdateRange(users);
```

### 🔍 Gelişmiş Sorgulama

```csharp
// Koşullu filtreleme
var query = _repository.GetAll()
    .WhereIf(!string.IsNullOrEmpty(searchTerm), u => u.Name.Contains(searchTerm))
    .WhereIf(activeOnly, u => u.IsActive);

// Sayfalama
var (users, totalCount) = await query.ToPaginatedListAsync(pageNumber, pageSize);
```

## 🚨 Hata Yönetimi

### 🛡️ Global Exception Handling

```csharp
// Otomatik hata yakalama ve yanıt oluşturma
throw new BusinessException("Geçersiz işlem");
throw new ValidationException("Email formatı hatalı");
throw new NotFoundException("User", userId);
throw new ConflictException("Email zaten mevcut");
```

### 📋 Hata Yanıt Formatı

```json
{
  "title": "Validation Error",
  "status": 400,
  "detail": "Bir veya daha fazla doğrulama hatası oluştu",
  "errorCode": "VALIDATION_ERROR",
  "traceId": "0HMVP9PQQFVS4:00000001",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

## 🐳 Deployment

### 🔧 Production Hazırlığı

```bash
# Veritabanı migration'ları
dotnet ef migrations add InitialCreate
dotnet ef database update

# Production build
dotnet publish -c Release -o ./publish

# Docker container
docker build -t clean-architecture-api .
docker run -p 8080:80 clean-architecture-api
```

### 🌍 Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Server=prod-server;Database=CleanArchDb;"
JwtSettings__SecretKey="ProductionSecretKey123!"
CacheProvider="Redis"
ConnectionStrings__Redis="prod-redis:6379"
```

## 📚 Geliştirme Kılavuzu

### 🎯 Yeni Özellik Ekleme

1. **Domain Layer** - Entity'nizi oluşturun
2. **Application Layer** - DTO'ları ve interface'leri ekleyin
3. **Infrastructure Layer** - Repository implementasyonunu yazın
4. **API Layer** - Controller endpoint'lerini oluşturun

### 🧪 Test Yazma

```csharp
// Unit test örneği
[Test]
public async Task GetUserAsync_WithValidId_ReturnsUser()
{
    // Arrange
    var userId = 1;
    var expectedUser = new UserDto { Id = userId, Name = "Test User" };
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(expectedUser);

    // Act
    var result = await _userService.GetUserAsync(userId);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(expectedUser.Id, result.Id);
}
```

### 🔍 Debugging İpuçları

- **Logging** - Serilog yapılandırılmış loglar üretir
- **Swagger** - API endpoint'lerini test edebilirsiniz
- **Developer Exception Page** - Geliştirme ortamında detaylı hata bilgileri
- **Entity Framework Logging** - SQL sorguları ve performans metrikleri

## 🔗 Yararlı Linkler

- **Clean Architecture** - [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- **Entity Framework Core** - [EF Core Docs](https://docs.microsoft.com/en-us/ef/core/)
- **JWT Authentication** - [JWT.io](https://jwt.io/)
- **Swagger/OpenAPI** - [Swagger Docs](https://swagger.io/docs/)
- **Serilog** - [Serilog Wiki](https://github.com/serilog/serilog/wiki)

## 🤝 Katkıda Bulunma

1. Bu repository'yi fork edin
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Branch'inizi push edin (`git push origin feature/AmazingFeature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasını inceleyebilirsiniz.

## 🙋‍♀️ Destek

Sorularınız için:
- **Issues** - [GitHub Issues](../../issues) bölümünü kullanın
- **Wiki** - [Proje Wiki](../../wiki) sayfalarını inceleyin
- **Discussions** - [GitHub Discussions](../../discussions) katılın

---

**💡 İpucu:** Projeyi incelemek için önce `src/API/Program.cs` dosyasından başlayın ve dependency injection yapılandırmasını gözden geçirin.