# VFS Disk Images - Setup Guide

## Problem: "No disks loaded" when running `vfslist`

Your `.img` files are in `guideXOS\disks\` on your **Windows host filesystem**, but guideXOS boots from a **ramdisk** (initrd) that doesn't include these files by default.

## Solution Options

### Option 1: Add images to your ramdisk/initrd (Proper solution)

Your ramdisk is built from files that are packaged into the bootloader. You need to:

1. **Find your ramdisk build directory**
   - Look for where your boot files are built (typically a `ramdisk/` or `initrd/` directory)
   - This is usually configured in your build/boot scripts

2. **Add a `/disks/` directory to the ramdisk**
   ```
   ramdisk/
     ??? disks/
         ??? test-fat32.img
         ??? test-ext4.img
   ```

3. **Rebuild your ramdisk/initrd**
   - This packages the directory into the bootable image
   - The exact command depends on your build system

4. **Reboot**
   - The images will now be available at `/disks/` when the OS boots
   - Auto-mount will find and mount them automatically

### Option 2: Manual mount from console (Quick test)

If your ramdisk **already** has some `.img` files somewhere:

```bash
# List what's on your ramdisk
ls /
ls /disks/

# If you find an .img file, manually mount it:
vfsmount /path/to/disk.img /mnt/test FAT32

# Then use it:
vfsuse /mnt/test
ls
```

### Option 3: Create images on the ramdisk at runtime

If your ramdisk has **writable** space, you could theoretically create small test images:

```bash
# This won't work directly since you can't run dd/mkfs in the OS
# But you could write a tool to create a minimal FAT image programmatically
```

### Option 4: Boot from HDD instead of ramdisk

If you're booting from an HDD/SSD with a real filesystem:

1. Put your `.img` files in `/disks/` on that filesystem
2. Boot from that disk (not ramdisk mode)
3. Auto-mount will work

## How to verify what's available

When the OS boots, look for these console messages:

```
[AutoMount] Initializing virtual disk auto-mount...
[AutoMount] Checking for /disks/test-fat32.img...
[AutoMount] /disks/test-fat32.img not found on ramdisk   <-- THIS means it's not there
[AutoMount] Checking for /disks/test-ext4.img...
[AutoMount] /disks/test-ext4.img not found on ramdisk
[AutoMount] Tip: Include .img files in your ramdisk/initrd to auto-mount at boot
[AutoMount] Auto-mount initialization complete (0 disks mounted)
```

If you see:
```
[AutoMount] Found /disks/test-fat32.img, auto-mounting...
[FileDisk] Loaded '/disks/test-fat32.img' (10485760 bytes, 20480 sectors)
[AutoMount] Mounted /disks/test-fat32.img at /mnt/fat32 as FAT32
```

Then it's working!

## Recommended approach for development

**For testing VFS commands right now:**

1. Create a **minimal test image programmatically** in your OS startup code
2. Or modify your ramdisk build to include the `.img` files

**Example: Create a small FAT image at runtime**

Add this to your `EntryPoint.cs` after `VirtualDiskAutoMount.Initialize()`:

```csharp
// Create a minimal 1MB test image in memory
byte[] testImage = new byte[1024 * 1024]; // 1 MB
// TODO: Format as FAT32 or just use raw data for testing

// Write to ramdisk
File.WriteAllBytes("/disks/test.img", testImage);

// Now auto-mount or manual mount will work
VirtualDiskAutoMount.MountVirtualDisk("/disks/test.img", "/mnt/test", "FAT32");
```

## Quick diagnostic commands

In the console, try:

```bash
# Check if /disks exists
ls /

# Try to create it
mkdir /disks   # (if mkdir is implemented)

# List what's actually on the ramdisk root
ls /

# Check if any .img files exist anywhere
# (manually browse with cd/ls)
```

## Summary

**The issue:** Your `.img` files are on the Windows filesystem, not in the ramdisk that the OS boots from.

**The fix:** Add the `.img` files to your ramdisk build, or create them programmatically at boot time.

**Immediate workaround:** Boot from HDD instead of ramdisk, or create test images at runtime.
