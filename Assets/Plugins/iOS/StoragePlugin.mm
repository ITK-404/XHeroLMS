// ============================================================
//  StoragePlugin.mm
//  Đặt file này vào:  Assets/Plugins/iOS/StoragePlugin.mm
//  Unity sẽ tự compile nó vào Xcode project khi build iOS.
// ============================================================

#import <Foundation/Foundation.h>

/// Trả về tổng dung lượng ổ đĩa (MB).
extern "C" float _GetTotalDiskSpaceMB() {
    NSError *error = nil;
    NSDictionary *attrs = [[NSFileManager defaultManager]
        attributesOfFileSystemForPath:NSHomeDirectory()
        error:&error];

    if (error || !attrs) return 0.f;

    NSNumber *total = attrs[NSFileSystemSize];
    return total ? (float)(total.longLongValue / (1024.0 * 1024.0)) : 0.f;
}

/// Trả về dung lượng còn trống (MB).
extern "C" float _GetFreeDiskSpaceMB() {
    NSError *error = nil;
    NSDictionary *attrs = [[NSFileManager defaultManager]
        attributesOfFileSystemForPath:NSHomeDirectory()
        error:&error];

    if (error || !attrs) return 0.f;

    // NSFileSystemFreeSize  = dung lượng trống vật lý
    // NSFileSystemFreeSize vs NSFileSystemFreeNodes:
    //   - FreeSize  → bytes còn dùng được (đúng với mục đích này)
    //   - FreeNodes → số inode còn trống (không cần thiết ở đây)
    NSNumber *free = attrs[NSFileSystemFreeSize];
    return free ? (float)(free.longLongValue / (1024.0 * 1024.0)) : 0.f;
}
