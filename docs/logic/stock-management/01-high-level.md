# ภาพรวมสถาปัตยกรรม — หน้าจัดการสต็อกสินค้า

สถาปัตยกรรมชั้นหลัก (Api / Application / Domain / Infrastructure) เหมือนกับที่อธิบายไว้ในเอกสารหัวข้อ
[sales-and-promotions](../01-high-level-architecture.md) ทุกประการ ต่างกันตรงที่หน้านี้เพิ่ม
component หนึ่งตัวที่หน้าขายไม่ได้ใช้ คือ **ที่เก็บไฟล์รูปภาพสินค้า (Product Image Store)**

```mermaid
graph TB
    subgraph Browser["เบราว์เซอร์ (แอดมินเท่านั้น)"]
        UI["StockLayout (การ์ดกัน role)\nweb/src/app/(app)/stock/layout.tsx"]
        Page["StockPage + ProductFormDialog\nweb/src/app/(app)/stock/page.tsx"]
        UI --> Page
    end

    subgraph API["api/ — ASP.NET Core Web API"]
        direction TB
        Ctrl["ProductsController\n[Authorize(Policy = \"Admin\")] บนทุก endpoint เขียนข้อมูล"]
        App["ManageProductUseCases (Create/Edit/Deactivate)\nSearchProductsQuery"]
        Dom["Product (Domain Entity)\nValidateInvariants, Deactivate, Edit,\nEnsureBarcodeIsAvailable, IsLowStock"]
        Infra["ProductRepository (EF Core)"]
        ImgStore["ProductImageStore\n(Infrastructure/Storage)"]
    end

    subgraph Disk["ไฟล์ระบบของเซิร์ฟเวอร์"]
        Files[("wwwroot/product-images/\n{guid}.{ext}")]
    end

    subgraph DB["PostgreSQL — ตาราง Products"]
        Tables[("Products\n(Name, Price, QuantityOnHand,\nBarcode, ImageUrl, LowStockThreshold, IsActive)")]
    end

    Page -- "REST/JSON + multipart/form-data\n(รูปภาพ)" --> Ctrl
    Ctrl --> App
    Ctrl -- "อัปโหลดไฟล์รูป" --> ImgStore
    ImgStore --> Files
    App --> Dom
    App --> Infra
    Infra --> Tables
```

## จุดที่ต่างจากหน้า Sales อย่างมีนัยสำคัญ

| ประเด็น | หน้า Sales (`/sales`) | หน้า Stock (`/stock`) |
|---|---|---|
| สิทธิ์การเข้าถึง | Cashier + Admin | **Admin เท่านั้น** |
| การ์ดกันสิทธิ์ฝั่งหน้าเว็บ | ไม่มี (ใช้ layout กลางของ `(app)` แค่เช็ค login) | `stock/layout.tsx` เช็ค `staff.role !== "Admin"` แล้ว `router.replace("/sales")` |
| พารามิเตอร์ค้นหาสินค้า | `includeInactive` ไม่ถูกส่ง (ปริยาย `false`) | ส่ง `includeInactive=true` เสมอ — เห็นสินค้าที่ปิดขายด้วย |
| การเขียนข้อมูล | ไม่มี (อ่านอย่างเดียว + สร้างบิลขาย) | สร้าง/แก้ไข/ปิดขายสินค้าโดยตรง |
| ไฟล์แนบ | ไม่มี | อัปโหลดรูปสินค้าแบบ `multipart/form-data` |

การกันสิทธิ์ฝั่งหน้าเว็บ (`StockLayout`) เป็นแค่ **ประสบการณ์ผู้ใช้** (กัน Cashier ไม่ให้เห็นหน้าที่ใช้ไม่ได้)
ตัวการันตีความปลอดภัยจริงคือฝั่ง API ที่มี `[Authorize(Policy = "Admin")]` กำกับทุก endpoint ที่เขียนข้อมูล
(`Create`, `Update`, `Delete`, `LowStock`) — ต่อให้ข้าม UI ยิง request ตรงก็ยังถูกปฏิเสธถ้าไม่ใช่ Admin
