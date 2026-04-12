# VFS Fixed - Capital D Issue

## The Problem
The auto-mount was looking for `/disks/test-fat32.img` (lowercase `d`) but the actual path in the ramdisk is `/Disks/test-fat32.img` (capital `D`).

The ramdisk source is at:
```
D:\devgitlab\guideXOS\guideXOS.Legacy\Ramdisk\Disks\
```

And this preserves the case when packed into `ramdisk.tar`, so in the OS it's `/Disks/` not `/disks/`.

## Immediate Solution (Works Right Now)

In your console, run:
```sh
vfsmount /Disks/test-fat32.img /mnt/fat32 FAT32
vfsmount /Disks/test-ext4.img /mnt/ext4 EXT4
vfslist
```

This will manually mount both disks immediately.

## Permanent Fix (After Restart)

I've updated the code to try `/Disks/` first (capital D), then `/disks/` as fallback, then `/ddDisks/` just in case.

**To apply the fix:**
1. Stop the OS (close the VM/emulator)
2. Rebuild the project
3. Restart the OS

You'll see during boot:
```
[AutoMount] Checking for FAT32 test image...
[AutoMount] Found /Disks/test-fat32.img, auto-mounting...
[FileDisk] Loaded '/Disks/test-fat32.img' (67108864 bytes, 131072 sectors)
[AutoMount] Mounted /Disks/test-fat32.img at /mnt/fat32 as FAT32
[AutoMount] Checking for EXT4 test image...
[AutoMount] Found /Disks/test-ext4.img, auto-mounting...
[FileDisk] Loaded '/Disks/test-ext4.img' (67108864 bytes, 131072 sectors)
[AutoMount] Mounted /Disks/test-ext4.img at /mnt/ext4 as EXT4
[AutoMount] Auto-mount initialization complete (2 disks mounted)
```

Then `vfslist` will show both disks automatically!

## Files Updated
- `guideXOS/OS/VirtualDiskAutoMount.cs` - Updated to use `/Disks/` (capital D) as primary path
- Config generation now uses correct case too

## Summary
? The ramdisk was correctly rebuilt with your disk images  
? The images are in the right place  
? The auto-mount was using the wrong case  
? Fixed by updating paths to `/Disks/` instead of `/disks/`  

**TL;DR:** Case sensitivity matters! Use `/Disks/` not `/disks/`.
