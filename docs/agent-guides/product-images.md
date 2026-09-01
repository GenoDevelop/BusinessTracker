# Product images — cross-cutting guide

Read this guide for product-image work in Domain, ApplicationLogic, Infrastructure, WPF, or tests.

- Store original binary content in PostgreSQL through `ProductImage`. List queries project only metadata; load selected full content separately.
- Accept JPEG, PNG, GIF, BMP, and TIFF. Limits are 10 MB per image, 20 images, and 50 MB per request.
- File pickers preflight the complete selection for count, per-file size, total size, and filename limits before allocating buffers. Read asynchronously, recheck opened-stream size, stage everything before mutating bound collections, and turn expected access/I/O failures into one validation error.
- Display oldest to newest and select the last newly uploaded image after upload. Users can save the selected original bytes and filename.
- Reuse `ProductImagesPanel` and `ProductImagesPopup`. The popup uses `PopupWindowHost` and retains percentage zoom controls.
- Management is available only from Products. Production, Recipes, and ordered Sales products open the same gallery read-only; entry buttons outside Products are disabled when there are no images.
