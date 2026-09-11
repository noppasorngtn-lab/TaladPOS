# กติกาทางธุรกิจของสินค้า (Product Lifecycle Logic)

ไฟล์หลักที่ถือกติกา: `api/src/TaladPOS.Domain/Entities/Product.cs` (Domain Entity — private setter
ทุกฟิลด์ แก้ไขได้ผ่านเมธอดที่ตรวจเงื่อนไขเท่านั้น ไม่มีทางเขียนค่าที่ผิดกติกาลงไปได้จากภายนอก)

## กติกาการสร้าง/แก้ไขสินค้า (`ValidateInvariants`)

เรียกทั้งตอน constructor (สร้างใหม่) และตอน `Edit()` (แก้ไข) — กติกาเดียวกันใช้ทั้งสองที่:

| เงื่อนไข | ผลถ้าไม่ผ่าน |
|---|---|
| `Name` ต้องไม่ว่าง/ไม่เป็นช่องว่างล้วน | `ArgumentException` |
| `Price` ต้องมากกว่า 0 | `ArgumentOutOfRangeException` |
| `QuantityOnHand` ต้องไม่ติดลบ | `ArgumentOutOfRangeException` |

Exception เหล่านี้ถูกแปลงเป็น HTTP 400 โดย error-handling middleware ของ API (ไม่ได้ตรวจซ้ำที่ Controller
หรือฝั่งหน้าเว็บ นอกจาก `required`/`min` ระดับพื้นฐานบน `<input>` ที่กันได้แค่กรณีง่ายๆ)

## บาร์โค้ดต้องไม่ซ้ำ — บังคับสองชั้น (defense in depth)

**ชั้นที่ 1 (Application layer, เพื่อ error message ที่อ่านง่าย):**
`ManageProductUseCases.EnsureBarcodeIsAvailableAsync()` เรียก
`ProductRepository.BarcodeExistsAsync(barcode, excludeProductId)` ก่อนบันทึกทุกครั้ง (ตอนแก้ไขจะ
`excludeProductId` เป็น id ของตัวเองเพื่อไม่ให้ชนกับบาร์โค้ดของตัวเอง) แล้วส่งผลไปให้
`Product.EnsureBarcodeIsAvailable(barcode, isTaken)` (static method บน Domain) ตัดสินใจว่าจะโยน
`ArgumentException` หรือไม่ — เป็น pure function ทดสอบได้โดยไม่ต้องมีฐานข้อมูลจริง

**ชั้นที่ 2 (Database, การันตีจริงแม้ request ชนกันพร้อมกัน):**
`ProductConfiguration.cs` (EF Core Fluent API) ประกาศ unique partial index บนคอลัมน์ `Barcode`:

```csharp
builder.HasIndex(p => p.Barcode)
    .IsUnique()
    .HasFilter("\"Barcode\" IS NOT NULL");
```

`HasFilter("\"Barcode\" IS NOT NULL")` ทำให้ index นี้ไม่บังคับความ unique กับแถวที่ `Barcode` เป็น
`NULL` (สินค้าที่ไม่มีบาร์โค้ดมีได้หลายชิ้น) แต่บาร์โค้ดที่มีค่าจริงจะซ้ำกันไม่ได้เด็ดขาดในระดับฐานข้อมูล
— ชั้นที่ 1 มีไว้ให้ประสบการณ์ผู้ใช้ดี (ข้อความ error ชัดเจน) ส่วนชั้นที่ 2 คือด่านที่ป้องกันจริงถ้าสอง
request มาพร้อมกันแล้วผ่านชั้นที่ 1 ไปทั้งคู่ (race condition) — ฐานข้อมูลจะปฏิเสธ request ที่สองด้วย
unique-violation แทน

## การอัปโหลดรูปสินค้า

- ไม่ได้เก็บรูปเป็น binary ในฐานข้อมูล — คอลัมน์ `Products.ImageUrl` เก็บแค่ **path สัมพัทธ์เป็น string**
  (เช่น `/product-images/3f9a....jpg`)
- ไฟล์จริงเก็บบนดิสก์ของเซิร์ฟเวอร์ที่ `wwwroot/product-images/` ผ่าน
  `ProductImageStore.SaveAsync()` (`api/src/TaladPOS.Infrastructure/Storage/ProductImageStore.cs`)
  — ตั้งชื่อไฟล์ใหม่ด้วย `Guid.NewGuid()` เสมอ (กันชื่อไฟล์ชนกัน/กันคนเดายื่อไฟล์ต้นฉบับจากชื่อ)
- เป็นการตัดสินใจทางสถาปัตยกรรมที่เหมาะกับสเกล "ร้านเดียว" (single-store) — ไม่ต้องพึ่ง cloud blob
  storage ภายนอก แต่ก็หมายความว่า **รูปภาพไม่ได้อยู่ใน backup ของฐานข้อมูล** ถ้า deploy เซิร์ฟเวอร์ใหม่
  หรือย้ายเครื่องต้องย้ายโฟลเดอร์ `wwwroot/product-images/` ไปด้วยเอง

## Soft-delete — ทำไมไม่ลบแถวจริง

`Product.Deactivate()` แค่ตั้ง `IsActive = false` ไม่มีการ `DELETE` แถวออกจากตาราง `Products`
เหตุผล (ตามคอมเมนต์ในโค้ด FR-011): ตาราง `SalesOrderLine` เก็บ `ProductNameSnapshot`/
`UnitPriceSnapshot` ไว้เองอยู่แล้วก็จริง แต่ยังมี **foreign key `ProductId`** ชี้กลับมาที่แถวสินค้านี้
— ถ้าลบแถวจริงจะทำให้ประวัติการขายเก่าอ้างอิง id ที่ไม่มีอยู่จริง (broken reference) จึงเลือก soft-delete
แทน ผลคือสินค้าที่ปิดขายแล้ว:
- **หายไปจากหน้าขาย** (`/sales` ค้นหาแบบ default ไม่รวม inactive)
- **ยังอยู่ในหน้า Stock** (แอดมินส่ง `includeInactive=true` เสมอ เห็นได้แต่แก้ไข/เปิดขายกลับไม่ได้ในตอนนี้
  — โค้ดปัจจุบันไม่มี endpoint "reactivate" ให้ย้อนกลับ `IsActive = true`)
- **บิลเก่าที่เคยขายสินค้านี้ยังแสดงผลถูกต้องเป๊ะ** ในหน้าประวัติการขาย เพราะอ่านจาก snapshot ไม่ใช่
  ข้อมูลปัจจุบันของสินค้า

## `IsLowStock` — คำนวณสด ไม่ใช่ค่าที่เก็บไว้

```csharp
public bool IsLowStock => LowStockThreshold.HasValue && QuantityOnHand <= LowStockThreshold.Value;
```

เป็น computed property คำนวณใหม่ทุกครั้งที่อ่าน ไม่ใช่คอลัมน์ในฐานข้อมูล — สินค้าที่ไม่ได้ตั้ง
`LowStockThreshold` ไว้ (เป็น `null`) จะไม่มีวัน "low stock" ไม่ว่าสต็อกจะเหลือเท่าไหร่ก็ตาม

## Sequence Diagram — สร้างสินค้าใหม่ (พร้อมอัปโหลดรูป)

```mermaid
sequenceDiagram
    participant UI as ProductFormDialog (React)
    participant Client as apiFetch (multipart/form-data)
    participant Ctrl as ProductsController
    participant ImgStore as ProductImageStore
    participant UC as ManageProductUseCases
    participant Repo as ProductRepository
    participant Prod as Product (Domain)
    participant DB as PostgreSQL
    participant Disk as wwwroot/product-images/

    UI->>Client: createProduct(token, values)
    Client->>Ctrl: POST /products (multipart: name, price, qty, barcode, threshold, image?)
    opt แนบไฟล์รูปมาด้วย
        Ctrl->>ImgStore: SaveAsync(stream, fileName)
        ImgStore->>Disk: เขียนไฟล์ {guid}.{ext}
        ImgStore-->>Ctrl: imageUrl (path สัมพัทธ์)
    end
    Ctrl->>UC: CreateAsync({ name, price, qty, imageUrl, barcode, threshold })
    UC->>Repo: BarcodeExistsAsync(barcode, excludeProductId: null)
    Repo->>DB: SELECT ... WHERE "Barcode" = @barcode
    UC->>Prod: Product.EnsureBarcodeIsAvailable(barcode, isTaken)
    alt บาร์โค้ดซ้ำ
        Prod-->>UC: ArgumentException → 400
    end
    UC->>Prod: new Product(name, price, qty, imageUrl, barcode, threshold)
    Note right of Prod: ValidateInvariants ตรวจ name/price/quantity ในตัว constructor
    UC->>Repo: AddAsync(product) + SaveChangesAsync()
    Repo->>DB: INSERT INTO "Products" (...)
    alt บาร์โค้ดชนกันพร้อมกัน (race condition หลุดผ่านเช็คชั้น 1)
        DB-->>Repo: unique index violation
        Repo-->>UC: DbUpdateException → 409/500
    end
    DB-->>Repo: บันทึกสำเร็จ
    Repo-->>UC: Product ที่บันทึกแล้ว
    UC-->>Ctrl: product
    Ctrl-->>Client: 201 Created (ProductDetailResponse)
    Client-->>UI: onSaved(product) → ปิด dialog, ตารางแม่ refresh()
```

## Sequence Diagram — ปิดการขายสินค้า (Soft Delete)

```mermaid
sequenceDiagram
    participant UI as StockPage
    participant Client as apiFetch
    participant Ctrl as ProductsController
    participant UC as ManageProductUseCases
    participant Repo as ProductRepository
    participant Prod as Product (Domain)
    participant DB as PostgreSQL

    UI->>UI: window.confirm("Remove ... ?")
    UI->>Client: deleteProduct(token, id)
    Client->>Ctrl: DELETE /products/{id}
    Ctrl->>UC: DeactivateAsync(id)
    UC->>Repo: GetByIdAsync(id)
    Repo->>DB: SELECT * FROM "Products" WHERE "Id" = @id
    alt ไม่พบสินค้า
        UC-->>Ctrl: KeyNotFoundException → 404
    end
    UC->>Prod: product.Deactivate()
    Note right of Prod: IsActive = false เท่านั้น ไม่มีการลบแถว
    UC->>Repo: SaveChangesAsync()
    Repo->>DB: UPDATE "Products" SET "IsActive" = false WHERE "Id" = @id
    Ctrl-->>Client: 204 No Content
    Client-->>UI: refresh() → สินค้ายังอยู่ในตาราง Stock แต่หายจากหน้า Sales
```
