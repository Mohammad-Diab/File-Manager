# File-Manager

A file manager for Windows with a built-in previewer and a set of file tools: big file finder, duplicate and empty folder removers, encryption, compression, secure delete, bulk renaming, a system monitor and a QEMU virtual machine launcher. Written in C# with WPF on .NET Framework 4.0.

## Features (v1.0.0)

- **Directory Explorer**: drives, quick access, back/forward/up, sorting, search in sub-folders, a Recent list (last 20 folders and 20 files), details of files and folders, hashes (MD5, SHA-1, SHA-256), attributes and dates, multi-rename, new files from the Windows templates, drag and drop with the Windows rules.
- **Safe delete**: Delete sends items to the Recycle Bin; Shift+Delete deletes permanently after its own question. Protected places (Windows, Program Files, user folders, whole drives) need a second "at my own risk" confirmation. Folder links (junctions, symbolic links) are never followed.
- **Copy and move** with progress, a speed graph and Stop; pasting into the same folder makes "name - Copy" like Windows; unfinished copies can be continued.
- **Preview** (press Space): folders, images, audio, video with subtitles, text, HTML, XPS documents, fonts, archives.
- **Tools**
  - Big File Finder
  - Identical File Remover: compares files by SHA-256 and always keeps the oldest copy
  - Empty Folder Remover
  - Files Encrypter: single files or one package for many files, optional encrypted names
  - Compressed Files Manager and Compress Files: 7z, zip, tar, gzip, bzip2 and xz, with passwords
  - File Destroyer and Destroy Disk: overwrite files, or the free space of a drive, before deleting
  - Renamer and extension changer
  - System Monitor
  - Virtual Machine and Create Virtual Hard Disk (QEMU)

### Encryption

v1.0.0 writes a new format: AES-256-CBC with an HMAC-SHA256 check per file, keys from PBKDF2 (200,000 iterations, random salt). Originals are deleted or overwritten only after every encrypted file has been decrypted again and checked. It still opens files made by older versions.

Files encrypted by v0.5.1 to v0.9.0 use a key that is built into the program (and is in this repository), so treat them as hidden, not as secure.

## Versions

| Version | What it is |
|---|---|
| v1.0.0 | Safe delete, AES-256 encryption, working compression and tools, new icons and sounds |
| v0.9.0 | Encryption rework: options, packages, encrypted names; decrypt and package fixes |
| v0.9.0-beta.1 | A snapshot of the v0.9.0 work |
| v0.8.1 | Directory Explorer toolbar, tool tiles, Options window |
| v0.8.0 | Directory Explorer, new message boxes, Operation rewrite, archive operations, language file stubs |
| v0.6.4 | Virtual Machine window, About window, 7-Zip checks |
| v0.5.1 | File Destroyer, Destroy Disk, Files Encrypter, the Preview window and the Icons library, Big File Finder, Empty Folder and Identical File removers, Compresser, System Monitor, Renamer |
| v0.3.0 | Viewer window (folders, images, audio, video, text, HTML, XPS/PDF, archives), new look |
| v0.2.0 | New folder, system information |
| v0.1.0 | Back, Forward and Refresh, details pane, hidden files, new window as administrator |
| v0.0.1 | Basic file manager: open files and browse folders |

The `-alpha.N` tags mark development states between these versions.

## Repository layout

| Folder | Content |
|---|---|
| `src` | The program: `File Manager.sln` with the File Manager project and the Icons library (`File Explorer.sln` up to v0.5.1-alpha.4) |
| `Languaes` | Arabic and English language file stubs (not loaded by the program) |
| `tools/Install-Fonts` | Console helper that installs fonts, used by Preview up to v0.9.0 |
| `experiments` | `WpfApplication3` (an interface experiment) and `Size-On-Disk` (VB and C# console tests) |
| `Version-2` | A modular rewrite (Core, Directories Explorer, Files Operations, PreView, Messaging Centre, File Manager) and a Music Player module; work in progress, it does not build as a whole |
| `docs` | Notes: the first release notes, the bug list (`Errors.txt`), ideas, file type tables, XAML drafts, and [`How To.mp4`](docs/How%20To.mp4), a short video that browses a folder and previews an image |
| `assets` | Icon drafts (SVG and PNG) |

## Requirements

Windows with .NET Framework 4.0 or later. Windows 8, 10 and 11 already include it.

The first versions (v0.0.1 to v0.3.0) targeted .NET Framework 4.5. From v0.5.1-alpha.1 (September 2013) the project moved down to .NET Framework 4.0 so it could also run on Windows XP SP3 and Windows Server 2003 SP2, which .NET 4.5 does not support. v1.0.0 still targets 4.0, but a few of its newer features need a newer Windows: the Office/PDF previews and the "Apps can open this type" list need Windows Vista or later, the GPU graphs need Windows 10 version 1709 or later, and QEMU hardware acceleration (WHPX) needs Windows 10 version 1803 or later.

Note: v1.0.0 keeps the date of the last real work on the project (2016), but it was completed in 2026 when the project was published. Some parts were added then so the final release works well on today's Windows: the GPU graphs, WHPX acceleration, the Fluent icons and the current 7-Zip engine.

The 7-Zip engine ships with v1.0.0. The Virtual Machine tool needs [QEMU](https://www.qemu.org/download/#windows); the tool offers the download page when QEMU is not installed.

## Usage

Download the `.7z` file of the version you want from [Releases](https://github.com/Mohammad-Diab/File-Manager/releases), extract it, and run `File Manager.exe`. There is no installer.

The exe is not signed, so Windows SmartScreen may warn the first time you run it: click **More info**, then **Run anyway**.

## Building

```
git clone https://github.com/Mohammad-Diab/File-Manager.git
cd File-Manager
```

Open `src\File Manager.sln` in Visual Studio 2017 or later and build, or run:

```
MSBuild "src\File Manager.sln" /restore /p:Configuration=Release
```

NuGet restores the .NET Framework 4.0 reference assemblies, SevenZipSharp and the 7-Zip engine. The program is written to `src\File Manager\bin\Release`.

Older tags (`git checkout v0.9.0`) target .NET Framework 4.0 (4.5 up to v0.3.0) and reference libraries that are not in the repository: SevenZipSharp, the Extended WPF Toolkit, the Foxit Reader ActiveX interop, Microsoft.Windows.Shell, and `Icons.dll` (built from `src\Icons`). Put them in one folder and pass it to MSBuild, together with the reference assemblies from the [Microsoft.NETFramework.ReferenceAssemblies.net40](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net40) package (`.net45` for v0.0.1 to v0.3.0):

```
MSBuild "src\File Manager\File Manager.csproj" /p:Configuration=Release /p:ReferencePath=<libraries>\ /p:TargetFrameworkRootPath=<package>\build
```

(`src\File Explorer\File Manager.csproj` up to v0.5.1-alpha.4.)

## Third-party files

The program uses 7-Zip (7z.dll), SevenZipSharp, Kenney Interface Sounds, Fluent UI System Icons and several icon themes. Their authors, licences and file lists are in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

The icon drafts in `assets` come from small-n-flat (CC0 1.0; `x-memory-file.svg` also uses a Fluent UI System Icons glyph, MIT), Tango (public domain), the Flat Circles icon pack by Vincent Winberg (CC BY-SA 3.0: `audio.png`, `image.png`, `photoshop.png`, `vedio.png`, `disk-vcd-file.svg`, `x-temp.svg`), the Humanity icon theme (GPL-2.0: `cd.png`) and the GNOME icon theme (CC BY-SA 3.0 or LGPL-3.0: `file-disk.png`). These files keep their own licences.

## Author

Mohammad Diab

## License

[Apache 2.0](LICENSE)
