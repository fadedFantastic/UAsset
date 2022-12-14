extern "C"
{
    uint64_t getAvailableDiskSpace () {
        uint64_t totalFreeSpace = 0;
        if (@available(iOS 11.0, *)) {
            NSURL *fileURL = [[NSURL alloc] initFileURLWithPath:NSHomeDirectory()];
            NSError *error = nil;
            NSDictionary *results = [fileURL resourceValuesForKeys:@[NSURLVolumeAvailableCapacityForImportantUsageKey] error:&error];
            if (!results) {
                NSLog(@"Error Obtaining System Memory Info: Domain = %@, Code = %ld", [error domain], (long)[error code]);
            } else {
                NSLog(@"Available capacity for important usage: %@", results[NSURLVolumeAvailableCapacityForImportantUsageKey]);
                totalFreeSpace = ((NSString *)results[NSURLVolumeAvailableCapacityForImportantUsageKey]).longLongValue;
            }
        } else {
            NSError *error = nil;
            NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
            NSDictionary *dictionary = [[NSFileManager defaultManager] attributesOfFileSystemForPath:[paths lastObject] error: &error];
            
            if (dictionary) {
                NSNumber *fileSystemSizeInBytes = [dictionary objectForKey: NSFileSystemSize];
                NSNumber *freeFileSystemSizeInBytes = [dictionary objectForKey:NSFileSystemFreeSize];
                totalFreeSpace = [freeFileSystemSizeInBytes unsignedLongLongValue];
            } else {
                NSLog(@"Error Obtaining System Memory Info: Domain = %@, Code = %ld", [error domain], (long)[error code]);
            }
            
        }
        return totalFreeSpace;
    }
}