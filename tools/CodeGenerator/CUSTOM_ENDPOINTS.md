# 🎯 Custom Endpoint Generator Kullanımı

Clean Architecture Code Generator artık CRUD işlemleri dışında özel endpoint'ler de oluşturabilir!

## 🚀 Temel Kullanım

### 1. **appsettings.json Konfigürasyonu**

```json
{
  "CodeGeneration": {
    "CustomActions": [
      {
        "Name": "GetActive",
        "Description": "Get active entities",
        "HttpMethod": "GET",
        "Route": "active",
        "ReturnType": "List",
        "Parameters": [],
        "AddCaching": true,
        "CacheKeyPattern": "{{EntityNameLower}}_active"
      },
      {
        "Name": "Activate",
        "Description": "Activate an entity",
        "HttpMethod": "POST", 
        "Route": "{id}/activate",
        "ReturnType": "Single",
        "Parameters": [{"Name": "id", "Type": "{{IdType}}"}],
        "AddLogging": true,
        "AddAudit": true
      },
      {
        "Name": "GetByStatus",
        "Description": "Get entities by status",
        "HttpMethod": "GET",
        "Route": "status/{status}",
        "ReturnType": "List",
        "Parameters": [
          {"Name": "status", "Type": "string", "FromRoute": true}
        ],
        "AddCaching": true,
        "AddPagination": true
      }
    ]
  }
}
```

### 2. **Komut Satırı Kullanımı**

```bash
# Tüm custom action'ları dahil et
dotnet run -- --entity Product

# Sadece belirli custom action'ları üret
dotnet run -- --entity Product --custom-actions GetActive,Activate

# Sadece custom action'lar (CRUD olmadan)
dotnet run -- --entity Product --actions-only

# Önizleme modu
dotnet run -- --entity Product --custom-actions GetActive --dry-run
```

## 📋 Custom Action Yapılandırması

### Temel Özellikler

| Özellik | Açıklama | Örnek |
|---------|----------|-------|
| `Name` | Method ismi | `"GetActive"` |
| `Description` | Method açıklaması | `"Get active entities"` |
| `HttpMethod` | HTTP verb | `"GET"`, `"POST"`, `"PUT"`, `"DELETE"` |
| `Route` | URL route | `"active"`, `"{id}/activate"` |
| `ReturnType` | Dönüş tipi | `"Single"`, `"List"`, `"Void"`, `"Custom"` |

### Parametre Konfigürasyonu

```json
{
  "Parameters": [
    {
      "Name": "id",
      "Type": "int",
      "IsRequired": true,
      "FromRoute": true
    },
    {
      "Name": "status", 
      "Type": "string",
      "FromQuery": true,
      "DefaultValue": "\"Active\""
    },
    {
      "Name": "request",
      "Type": "UpdateStatusRequest",
      "FromBody": true
    }
  ]
}
```

### Özellik Bayrakları

```json
{
  "AddCaching": true,
  "CacheKeyPattern": "product_{{status}}",
  "AddLogging": true,
  "AddAudit": true,
  "AddPagination": true
}
```

## 🎯 Kullanım Örnekleri

### Örnek 1: Basit GET Endpoint

```json
{
  "Name": "GetActive",
  "Description": "Get all active products",
  "HttpMethod": "GET",
  "Route": "active",
  "ReturnType": "List",
  "AddCaching": true,
  "CacheKeyPattern": "products_active"
}
```

**Üretilen Controller:**
```csharp
[HttpGet("active")]
[Cache("products_active", ExpirationMinutes = 30)]
[Log(LogLevel.Information, LogExecutionTime = true)]
public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetActiveAsync()
{
    var result = await _productService.GetActiveAsync();
    if (!result.IsSuccess)
        return BadRequest(result.ErrorMessage);
    return Ok(result.Data);
}
```

### Örnek 2: Parametreli POST Endpoint

```json
{
  "Name": "UpdateStatus",
  "Description": "Update product status",
  "HttpMethod": "POST",
  "Route": "{id}/status",
  "ReturnType": "Single",
  "Parameters": [
    {"Name": "id", "Type": "int", "FromRoute": true},
    {"Name": "status", "Type": "string", "FromBody": true}
  ],
  "AddAudit": true,
  "AddLogging": true
}
```

**Üretilen Controller:**
```csharp
[HttpPost("{id}/status")]
[Log(LogLevel.Information, LogExecutionTime = true)]
[LogAudit("POST", "Product")]
public async Task<ActionResult<ProductResponseDto>> UpdateStatusAsync(
    [FromRoute] int id, 
    [FromBody] string status)
{
    var result = await _productService.UpdateStatusAsync(id, status);
    if (!result.IsSuccess)
        return BadRequest(result.ErrorMessage);
    if (result.Data == null)
        return NotFound();
    return Ok(result.Data);
}
```

### Örnek 3: Sayfalama ile Liste

```json
{
  "Name": "GetByCategory",
  "Description": "Get products by category with pagination",
  "HttpMethod": "GET", 
  "Route": "category/{categoryId}",
  "ReturnType": "List",
  "Parameters": [
    {"Name": "categoryId", "Type": "int", "FromRoute": true}
  ],
  "AddPagination": true,
  "AddCaching": true,
  "CacheKeyPattern": "products_category_{{categoryId}}"
}
```

**Üretilen Controller:**
```csharp
[HttpGet("category/{categoryId}")]
[Cache("products_category_{{categoryId}}", ExpirationMinutes = 30, KeyParameters = new[] { "categoryId" })]
public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetByCategoryAsync(
    [FromRoute] int categoryId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var result = await _productService.GetByCategoryAsync(categoryId, pageNumber, pageSize);
    if (!result.IsSuccess)
        return BadRequest(result.ErrorMessage);
    return Ok(result.Data);
}
```

## 🛠️ Gelişmiş Konfigürasyon

### Custom Return Type

```json
{
  "Name": "GetStatistics",
  "Description": "Get product statistics",
  "HttpMethod": "GET",
  "Route": "statistics",
  "ReturnType": "Custom",
  "CustomReturnType": "ProductStatisticsDto",
  "AddCaching": true
}
```

### Complex Parameters

```json
{
  "Name": "BulkUpdate",
  "Description": "Bulk update products",
  "HttpMethod": "PUT",
  "Route": "bulk",
  "ReturnType": "Void",
  "Parameters": [
    {
      "Name": "request",
      "Type": "BulkUpdateRequest",
      "FromBody": true
    }
  ],
  "AddLogging": true,
  "AddAudit": true
}
```

## 🎨 Template Özelleştirme

Custom action template'leri `Templates/` klasöründe:

- `CustomAction.template` - Controller action
- `CustomServiceMethod.template` - Service method

### Template Değişkenleri

```
{{EntityName}} - Product
{{EntityNameLower}} - product  
{{EntityNameCamel}} - product
{{EntityNamePlural}} - Products
{{IdType}} - int, Guid
{{HttpMethod}} - GET, POST, PUT, DELETE
{{Name}} - Method ismi
{{Description}} - Method açıklaması
{{Route}} - URL route
```

### Conditional Blocks

```
{{#AddCaching}}[Cache(...)]{{/AddCaching}}
{{#AddLogging}}[Log(...)]{{/AddLogging}}
{{#AddAudit}}[LogAudit(...)]{{/AddAudit}}
{{#AddPagination}}pageNumber, pageSize{{/AddPagination}}
```

## 🚀 Gerçek Dünya Örnekleri

### E-Commerce Senaryosu

```json
{
  "CustomActions": [
    {
      "Name": "GetFeatured",
      "HttpMethod": "GET",
      "Route": "featured",
      "ReturnType": "List",
      "AddCaching": true
    },
    {
      "Name": "UpdatePrice", 
      "HttpMethod": "PUT",
      "Route": "{id}/price",
      "Parameters": [
        {"Name": "id", "Type": "int", "FromRoute": true},
        {"Name": "price", "Type": "decimal", "FromBody": true}
      ],
      "ReturnType": "Single",
      "AddAudit": true
    },
    {
      "Name": "GetLowStock",
      "HttpMethod": "GET", 
      "Route": "low-stock",
      "Parameters": [
        {"Name": "threshold", "Type": "int", "FromQuery": true, "DefaultValue": "10"}
      ],
      "ReturnType": "List",
      "AddCaching": true
    }
  ]
}
```

### Blog Senaryosu

```json
{
  "CustomActions": [
    {
      "Name": "GetPublished",
      "HttpMethod": "GET",
      "Route": "published", 
      "ReturnType": "List",
      "AddPagination": true,
      "AddCaching": true
    },
    {
      "Name": "Publish",
      "HttpMethod": "POST",
      "Route": "{id}/publish",
      "Parameters": [{"Name": "id", "Type": "int", "FromRoute": true}],
      "ReturnType": "Single",
      "AddAudit": true
    },
    {
      "Name": "GetByTag",
      "HttpMethod": "GET",
      "Route": "tag/{tag}",
      "Parameters": [{"Name": "tag", "Type": "string", "FromRoute": true}],
      "ReturnType": "List",
      "AddCaching": true,
      "AddPagination": true
    }
  ]
}
```

## 💡 Best Practices

1. **Naming Convention**: PascalCase method isimleri kullanın
2. **Route Design**: RESTful route yapısını takip edin
3. **Caching**: GET method'ları için cache aktif edin
4. **Logging**: Önemli işlemler için logging ekleyin
5. **Audit**: Veri değiştiren işlemler için audit aktif edin
6. **Pagination**: Liste dönen method'lar için pagination kullanın

## 🔧 Troubleshooting

### Custom Action Görünmüyor
```bash
# appsettings.json syntax'ını kontrol edin
# Build edip tekrar deneyin
dotnet build
dotnet run -- --entity Product --dry-run
```

### Template Hataları
- Template dosyalarının `Templates/` klasöründe olduğundan emin olun
- Template syntax'ını kontrol edin
- Build output'ta template'lerin kopyalandığını doğrulayın

---

**🎯 Sonuç:** Custom endpoint generator ile artık CRUD dışında istediğiniz endpoint'leri de otomatik olarak oluşturabilirsiniz. Configuration-based yaklaşım sayesinde kod yazmadan sadece JSON ile tanımlama yapabilirsiniz!