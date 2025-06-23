# 🔧 Clean Architecture Code Generator

Clean Architecture projeleriniz için otomatik CRUD kod üretici. Entity'lerinize göre Controller, Service, Repository, DTO ve custom endpoint'leri otomatik olarak oluşturur.

## 🚀 Özellikler

- ✅ **Entity Analysis:** Domain entity'lerini otomatik analiz eder
- ✅ **Full CRUD Generation:** Create, Read, Update, Delete işlemleri
- ✅ **Custom Endpoints:** CRUD dışında özel endpoint'ler
- ✅ **Clean Architecture:** Doğru katmanlara kod yerleştirme
- ✅ **Attribute-Based Programming:** Cache, Logging, Audit attribute'leri
- ✅ **Auto Project Integration:** Program.cs, UnitOfWork, AutoMapper otomatik güncelleme
- ✅ **Template Engine:** Özelleştirilebilir template'ler
- ✅ **Smart Features:** Pagination, filtering, validation
- ✅ **Global Tool:** Herhangi bir projede kullanılabilir
- ✅ **Preview Mode:** Dry-run ile önizleme

## 📦 Kurulum

### 🌍 Global Tool Kurulumu (Önerilen)

```bash
# CodeGenerator dizininden pack ve install
cd tools/CodeGenerator
dotnet pack
dotnet tool install --global --add-source ./bin/Release CleanArchitecture.CodeGenerator

# Artık herhangi bir projede kullanabilirsiniz
ca-generator --help
```

### 🔧 Local Development Kurulumu

```bash
cd tools/CodeGenerator
dotnet restore
dotnet build
```

## 🎯 Kullanım

### 🌍 Global Tool Kullanımı (Herhangi Bir Projede)

```bash
# Proje ana dizininde (src/ klasörü olan yerde)
cd /path/to/your-clean-architecture-project

# Entity'leri listele
ca-generator

# CRUD kod üret
ca-generator --entity Product
ca-generator --entity Order --overwrite
ca-generator --entity User --dry-run

# Custom actions ile
ca-generator --entity Product --custom-actions GetActive,Activate
ca-generator --entity Order --actions-only
```

### 🔧 Local Development Kullanımı
```bash
dotnet run --entity Product
dotnet run --entity Order --overwrite
dotnet run --entity Category --dry-run
```

### Seçenekler
```bash
# Sadece controller üret
dotnet run --entity Product --controller-only

# Sadece service katmanı üret
dotnet run --entity Product --service-only

# Cache özelliklerini deaktif et
dotnet run --entity Product --no-cache

# Mevcut dosyaları üzerine yaz
dotnet run --entity Product --overwrite

# Sadece önizleme (dosya yazmaz)
dotnet run --entity Product --dry-run
```

## 📁 Üretilen Dosyalar

### DTOs (Application/DTOs/)
- `CreateProductDto.cs` - Oluşturma için DTO
- `UpdateProductDto.cs` - Güncelleme için DTO
- `ProductResponseDto.cs` - Response DTO

### Repository (Infrastructure/Repositories/)
- `IProductRepository.cs` - Repository interface
- `ProductRepository.cs` - Repository implementation

### Service (Infrastructure/Services/)
- `IProductService.cs` - Service interface
- `ProductService.cs` - Service implementation

### Controller (API/Controllers/)
- `ProductsController.cs` - REST API endpoints

### AutoMapper (API/Mappings/)
- `ProductMappingProfile.cs` - Object mapping configuration

## 🛠️ Konfigürasyon

### 🌍 Global Tool Konfigürasyonu

Herhangi bir projede `ca-generator.json` oluşturarak özelleştirebilirsiniz:

```json
{
  "CodeGeneration": {
    "ProjectRoot": "./src",
    "Namespace": {
      "Domain": "YourProject.Domain.Entities",
      "Application": "YourProject.Application",
      "Infrastructure": "YourProject.Infrastructure",
      "API": "YourProject.API.Controllers"
    },
    "Features": {
      "GenerateRepository": true,
      "GenerateService": true,
      "GenerateController": true,
      "GenerateDTOs": true,
      "GenerateMapper": true,
      "AddCaching": true,
      "AddLogging": true,
      "AddValidation": true,
      "AddAudit": true
    },
    "CustomActions": [
      {
        "Name": "GetActive",
        "Description": "Get active entities",
        "HttpMethod": "GET",
        "Route": "active",
        "ReturnType": "List",
        "AddCaching": true,
        "CacheKeyPattern": "{{EntityNameLower}}_active"
      },
      {
        "Name": "Activate",
        "Description": "Activate an entity",
        "HttpMethod": "POST",
        "Route": "{id}/activate",
        "ReturnType": "Single",
        "Parameters": [{"Name": "id", "Type": "{{IdType}}", "FromRoute": true}],
        "AddLogging": true,
        "AddAudit": true
      },
      {
        "Name": "GetByStatus",
        "Description": "Get entities by status",
        "HttpMethod": "GET",
        "Route": "status/{status}",
        "ReturnType": "List",
        "Parameters": [{"Name": "status", "Type": "string", "FromRoute": true}],
        "AddCaching": true,
        "AddPagination": true
      }
    ]
  }
}
```

### 🔧 Local Development Konfigürasyonu (appsettings.json)

```json
{
  "CodeGeneration": {
    "ProjectRoot": "../../src",
    "Namespace": {
      "Domain": "Domain.Entities",
      "Application": "Application",
      "Infrastructure": "Infrastructure",
      "API": "API.Controllers"
    },
    "Features": {
      "GenerateRepository": true,
      "GenerateService": true,
      "GenerateController": true,
      "GenerateDTOs": true,
      "GenerateMapper": true,
      "AddCaching": true,
      "AddLogging": true,
      "AddValidation": true,
      "AddAudit": true
    }
  }
}
```

## 📋 Template'ler

Template'ler `Templates/` klasöründe bulunur:

- `CreateDto.template` - Create DTO template
- `UpdateDto.template` - Update DTO template
- `ResponseDto.template` - Response DTO template
- `Repository.template` - Repository implementation
- `RepositoryInterface.template` - Repository interface
- `Service.template` - Service implementation
- `ServiceInterface.template` - Service interface
- `Controller.template` - API Controller
- `MappingProfile.template` - AutoMapper profile

### Template Syntax

```
{{EntityName}} - Entity ismi (Product)
{{EntityNamePlural}} - Çoğul form (Products)
{{EntityNameLower}} - Küçük harf (product)
{{EntityNameCamel}} - CamelCase (product)
{{IdType}} - ID property tipi (int, Guid)
{{DomainNamespace}} - Domain namespace

{{#HasCaching}}...{{/HasCaching}} - Conditional blocks
{{#Properties}}...{{/Properties}} - Property loops
```

## 🎯 Örnek: Product Entity

### Entity
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Üretilen Controller
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Log(LogLevel.Information, LogExecutionTime = true)]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Cache("products", ExpirationMinutes = 5)]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
    
    [HttpGet("{id}")]
    [Cache("product", KeyParameters = new[] { "id" })]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
    
    [HttpPost]
    [CacheEvict(KeyPatterns = new[] { "products" })]
    [LogAudit("CREATE", "Product")]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] CreateProductDto dto)
    
    // PUT, DELETE endpoints...
}
```

### Üretilen Service
```csharp
public class ProductService : IProductService
{
    [Cache("products", ExpirationMinutes = 10)]
    [Log(LogLevel.Information)]
    public async Task<Result<IEnumerable<ProductResponseDto>>> GetAllProductsAsync()
    
    [CacheEvict(KeyPatterns = new[] { "products", "product_*" })]
    [LogAudit("CREATE", "Product")]
    public async Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductDto dto)
    
    // Other CRUD methods...
}
```

## 🔄 Otomatik Integration

🎉 **Artık manual integration gerekmiyor!** Generator otomatik olarak aşağıdaki dosyaları günceller:

### ✅ Otomatik Güncellenen Dosyalar
1. **Program.cs** - Service kayıtları eklenir
2. **UnitOfWork Interface & Implementation** - Repository property'si eklenir  
3. **AutoMapper Configuration** - Mapping profili kaydedilir

### 🔧 Manuel Integration (Eski Yöntem)
Eğer `--no-integration` flag'i kullanırsanız, manuel olarak ekleyebilirsiniz:

```csharp
// Program.cs
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddFullInterception<IProductService, ProductService>();

// UnitOfWork
IProductRepository Products { get; }

// AutoMapper
builder.Services.AddAutoMapper(typeof(ProductMappingProfile));
```

## 📊 Üretilen API Endpoints

### 🔧 Standart CRUD Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/products` | Tüm ürünleri listele |
| `GET` | `/api/v1/products/paged` | Sayfalı ürün listesi |
| `GET` | `/api/v1/products/{id}` | ID'ye göre ürün getir |
| `POST` | `/api/v1/products` | Yeni ürün oluştur |
| `PUT` | `/api/v1/products/{id}` | Ürünü güncelle |
| `DELETE` | `/api/v1/products/{id}` | Ürünü sil |

### 🎯 Custom Endpoints

Custom action'lar ile ek endpoint'ler oluşturabilirsiniz:

| Method | Endpoint | Description | Command |
|--------|----------|-------------|---------|
| `GET` | `/api/v1/products/active` | Aktif ürünleri listele | GetActive action |
| `POST` | `/api/v1/products/{id}/activate` | Ürünü aktifleştir | Activate action |
| `GET` | `/api/v1/products/status/{status}` | Duruma göre ürünler | GetByStatus action |

#### Custom Action Komutları:
```bash
# Tüm custom action'lar ile
ca-generator --entity Product

# Belirli custom action'lar ile
ca-generator --entity Product --custom-actions GetActive,Activate

# Sadece custom action'lar (CRUD olmadan)
ca-generator --entity Product --actions-only
```

## 🎨 Özelleştirme

### Custom Template Oluşturma
1. `Templates/` klasörüne yeni `.template` dosyası ekle
2. Template syntax kullanarak içeriği yaz
3. `ICodeGenerator.GenerateAllAsync()` method'unu güncelle

### Feature Flag'leri
```bash
# Sadece temel CRUD, cache/logging olmadan
dotnet run --entity Product --no-cache --no-logging --no-audit

# Sadece API katmanı
dotnet run --entity Product --controller-only
```

## 📝 Best Practices

1. **Entity Design:** Clean Architecture prensiplerine uygun entity'ler oluşturun
2. **Validation:** Entity'lerde Data Annotations kullanın
3. **Naming:** PascalCase entity isimleri kullanın
4. **Navigation Properties:** Virtual navigation property'leri tanımlayın
5. **Audit Fields:** CreatedAt, UpdatedAt gibi audit alanları ekleyin

## 🐛 Troubleshooting

### 🌍 Global Tool Sorunları

#### 1. Tool Kurulumu
```bash
# Tool kurulumunu kontrol et
dotnet tool list -g | grep CleanArchitecture

# Tool'u güncelle
dotnet tool uninstall -g CleanArchitecture.CodeGenerator
cd tools/CodeGenerator
dotnet pack
dotnet tool install --global --add-source ./bin/Release CleanArchitecture.CodeGenerator
```

#### 2. Global Tool Kullanımı
```bash
# ✅ DOĞRU: Proje ana dizininde (src/ klasörü olan yerde)
cd /path/to/your-clean-architecture-project
ca-generator --entity Product

# ✅ DOĞRU: Farklı output path ile
ca-generator --entity Product --output ./source

# ❌ YANLIŞ: src/ klasörü olmayan dizinde
cd /random/directory
ca-generator --entity Product  # Domain.dll bulunamaz
```

#### 3. Local Development Sorunları
```bash
# ✅ DOĞRU: Generator kendi dizininden çalıştırın
cd tools/CodeGenerator
dotnet run -- --entity Product

# ✅ DOĞRU: Proje root'undan project parametresi ile
dotnet run --project tools/CodeGenerator -- --entity Product
```

### Domain Assembly Bulunamıyor

#### 🌍 Global Tool İçin:
```bash
# Proje ana dizininde Domain projesini build edin
cd /path/to/your-project
dotnet build src/Domain

# Generator otomatik olarak bu yolları arar:
# • src/Domain/bin/Debug/net8.0/Domain.dll
# • src/Domain/bin/Release/net8.0/Domain.dll
# • Domain.dll (current directory)
```

#### 🔧 Local Development İçin:
```bash
# Domain projesini build edin
cd ../../src/Domain
dotnet build

# DLL'i code generator'a kopyalayın (opsiyonel)
cp bin/Debug/net8.0/Domain.dll ../../tools/CodeGenerator/bin/Debug/net8.0/
```

### Proje Yapısı Sorunları

#### Beklenen Dizin Yapısı:
```
MyProject/
├── src/
│   ├── API/           # Controllers buraya yazılır
│   ├── Application/   # DTOs, Interfaces buraya yazılır  
│   ├── Domain/        # Entities burada okunur
│   ├── Infrastructure/ # Repositories, Services buraya yazılır
│   └── Common/        # Shared components
└── tools/
    └── CodeGenerator/ # Generator buradan çalıştırılır
        ├── appsettings.json
        └── Program.cs
```

#### Dizin Kontrolü:
```bash
cd tools/CodeGenerator
# Bu komutla src klasörlerini görebilmeniz lazım:
ls ../../src
# Çıktı: API  Application  Common  Domain  Infrastructure
```

### appsettings.json Konfigürasyonu

Eğer farklı bir proje yapınız varsa, `appsettings.json` dosyasını güncelleyin:

```json
{
  "CodeGeneration": {
    "ProjectRoot": "../../src",     // src klasörünün relative path'i
    "Namespace": {
      "Domain": "YourProject.Domain.Entities",
      "Application": "YourProject.Application", 
      "Infrastructure": "YourProject.Infrastructure",
      "API": "YourProject.API.Controllers"
    }
  }
}
```

### Template Bulunamıyor
- `Templates/` klasöründe doğru `.template` dosyalarının olduğundan emin olun
- Build output'ta template dosyalarının kopyalandığını kontrol edin

### Namespace Hataları
- `appsettings.json` dosyasındaki namespace ayarlarını kontrol edin
- Projenizin namespace yapısına uygun olarak güncelleyin

## 🚀 Gelecek Özellikler

- [ ] **NuGet Package:** Official NuGet release
- [ ] **GUI Interface:** Web-based code generator
- [ ] **Unit Test Generation:** Otomatik test oluşturma
- [ ] **Migration Scripts:** Database migration support
- [ ] **Swagger Documentation:** Enhanced API docs
- [ ] **Bulk Operations:** Multi-entity generation
- [ ] **Custom Validation Rules:** Advanced validation templates
- [ ] **Integration with VS/VSCode:** Editor extensions

---

## 🎉 Özet

**Clean Architecture Code Generator** artık tamamen hazır! 

### ✅ **Özellikler:**
- 🌍 **Global Tool** - Herhangi bir Clean Architecture projesinde kullanabilirsiniz
- 🎯 **Custom Endpoints** - CRUD dışında özel endpoint'ler oluşturabilirsiniz  
- 🔄 **Otomatik Integration** - Program.cs, UnitOfWork, AutoMapper otomatik güncellenir
- 📁 **Smart Assembly Detection** - Domain.dll'i otomatik bulur
- ⚙️ **Configurable** - `ca-generator.json` ile tam özelleştirme

### 🚀 **Hızlı Başlangıç:**
```bash
# 1. Global tool kurulumu (tek seferlik)
cd tools/CodeGenerator
dotnet pack
dotnet tool install --global --add-source ./bin/Release CleanArchitecture.CodeGenerator

# 2. Herhangi bir Clean Architecture projesinde kullanım
cd /path/to/any-project
ca-generator --entity Product

# 3. Custom endpoint'lerle
ca-generator --entity Order --custom-actions GetActive,Activate
```

**💡 İpucu:** `ca-generator --help` ile tüm seçenekleri görebilir, `--dry-run` ile önizleme yapabilirsiniz.