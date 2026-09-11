# ตารางฐานข้อมูล — ส่วนที่เพิ่มเติมสำหรับหน้าจัดการสต็อกสินค้า

ตาราง `Products` ถูกอธิบายไว้แล้วอย่างละเอียด (คอลัมน์ทั้งหมด, ER diagram, ใครอ่าน/เขียนบ้าง) ที่เอกสาร
[`../04-data-model.md`](../04-data-model.md) ของหัวข้อ sales-and-promotions — ไฟล์นี้เสริมเฉพาะรายละเอียด
ที่เกี่ยวกับหน้า Stock โดยเฉพาะซึ่งเอกสารหัวข้อก่อนหน้าไม่ได้ลงลึก

## Unique constraint บนคอลัมน์ `Barcode`

ประกาศไว้ที่ `api/src/TaladPOS.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`:

```sql
CREATE UNIQUE INDEX ... ON "Products" ("Barcode") WHERE "Barcode" IS NOT NULL;
```

เป็น **partial unique index** — บังคับ unique เฉพาะแถวที่ `Barcode` ไม่ใช่ `NULL` เท่านั้น สินค้าที่ไม่มี
บาร์โค้ด (`NULL`) มีได้หลายชิ้นพร้อมกันโดยไม่ชนกัน ดูรายละเอียดกลไกการตรวจสอบสองชั้นที่
[`03-product-lifecycle-logic.md`](./03-product-lifecycle-logic.md)

## คอลัมน์ `ImageUrl` ไม่ได้เก็บไฟล์จริง

`Products.ImageUrl` (nullable, `HasMaxLength(500)`) เก็บแค่ **string path สัมพัทธ์** ที่ชี้ไปยังไฟล์บนดิสก์
ของเซิร์ฟเวอร์ (`wwwroot/product-images/{guid}.{ext}`) — ไม่มีการเก็บ binary/BLOB ใดๆ ในฐานข้อมูล
หมายความว่าการ backup ฐานข้อมูลเพียงอย่างเดียว **ไม่ครอบคลุมไฟล์รูปภาพ** ต้อง backup โฟลเดอร์
`wwwroot/product-images/` แยกต่างหากด้วยหากต้องการกู้คืนระบบให้สมบูรณ์

## ใครเขียนคอลัมน์ไหนบ้างจากหน้า Stock (สรุปเพิ่มจากเอกสารหัวข้อ sales-and-promotions)

| คอลัมน์ | เขียนจากจุดไหน |
|---|---|
| `Name`, `Price`, `QuantityOnHand`, `Barcode`, `LowStockThreshold`, `ImageUrl` | `Product.Edit()` เรียกจาก `ManageProductUseCases.EditAsync` (หน้า Stock → PUT /products/{id}) |
| ทั้งหมดข้างต้น (ตอนสร้างครั้งแรก) | `Product` constructor เรียกจาก `ManageProductUseCases.CreateAsync` (หน้า Stock → POST /products) |
| `IsActive` | `Product.Deactivate()` เรียกจาก `ManageProductUseCases.DeactivateAsync` (หน้า Stock → DELETE /products/{id}) เท่านั้น — ไม่มีจุดไหนอื่นในโค้ดปัจจุบันที่ตั้งค่านี้กลับเป็น `true` ได้ (ไม่มี endpoint reactivate) |
| `QuantityOnHand` (ตัดผ่าน checkout/void) | มาจากหน้า Sales ไม่ใช่หน้า Stock — ดู [`../03-promotion-pricing.md`](../03-promotion-pricing.md) |
