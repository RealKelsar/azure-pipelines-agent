// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Agent.Sdk;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public sealed class IOUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Delete_DeletesDirectory()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.Delete(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Delete_DeletesFile()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.Delete(file, CancellationToken.None);

                    // Assert.
                    Assert.False(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void DeleteDirectory_DeleteTargetFileWithASymlink()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string targetFile = Path.Combine(directory, "somefile");
                string symlink = Path.Combine(directory, "symlink");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: targetFile, contents: "some contents");
                    File.SetAttributes(targetFile, File.GetAttributes(targetFile) | FileAttributes.ReadOnly);

                    await CreateFileReparsePoint(context: hc, link: symlink, target: targetFile);

                    // Act.
                    IOUtil.DeleteFile(targetFile);
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(File.Exists(targetFile));
                    Assert.False(File.Exists(symlink));

                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesDirectoriesRecursively()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(Path.Combine(directory, "some child directory", "some grandchild directory"));

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DeletesDirectoryReparsePointChain()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/<guid 1> -> <guid 2>
                //   randomDir/<guid 2> -> <guid 3>
                //   randomDir/<guid 3> -> <guid 4>
                //   randomDir/<guid 4> -> <guid 5>
                //   randomDir/<guid 5> -> targetDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //
                // The purpose of this test is to verify that DirectoryNotFoundException is gracefully handled when
                // deleting a chain of reparse point directories. Since the reparse points are named in a random order,
                // the DirectoryNotFoundException case is likely to be encountered.
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir1 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir1");
                    string linkDir2 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir2");
                    string linkDir3 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir3");
                    string linkDir4 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir4");
                    string linkDir5 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir5");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir1, target: linkDir2);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir2, target: linkDir3);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir3, target: linkDir4);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir4, target: linkDir5);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir5, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir1));
                    Assert.True(new DirectoryInfo(linkDir1).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir1, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(randomDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir1));
                    Assert.False(Directory.Exists(targetDir));
                    Assert.False(File.Exists(file));
                    Assert.False(Directory.Exists(randomDir));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DeletesDirectoryReparsePointsBeforeDirectories()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/linkDir -> targetDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //
                // The accuracy of this test relies on an assumption that IOUtil sorts the directories in
                // descending order before deleting them - either by length or by default sort order.
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir = Path.Combine(randomDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(randomDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir));
                    Assert.False(Directory.Exists(targetDir));
                    Assert.False(File.Exists(file));
                    Assert.False(Directory.Exists(randomDir));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesFilesRecursively()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string file = Path.Combine(directory, "some subdirectory", "some file");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesReadOnlyDirectories()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only subdirectory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string subdirectory = Path.Combine(directory, "some subdirectory");
                try
                {
                    var subdirectoryInfo = new DirectoryInfo(subdirectory);
                    subdirectoryInfo.Create();
                    subdirectoryInfo.Attributes = subdirectoryInfo.Attributes | FileAttributes.ReadOnly;

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    var subdirectoryInfo = new DirectoryInfo(subdirectory);
                    if (subdirectoryInfo.Exists)
                    {
                        subdirectoryInfo.Attributes = subdirectoryInfo.Attributes & ~FileAttributes.ReadOnly;
                    }

                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesReadOnlyRootDirectory()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a read-only directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    directoryInfo.Create();
                    directoryInfo.Attributes = directoryInfo.Attributes | FileAttributes.ReadOnly;

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    var directoryInfo = new DirectoryInfo(directory);
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Attributes = directoryInfo.Attributes & ~FileAttributes.ReadOnly;
                        directoryInfo.Delete();
                    }
                }
            }
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void DeleteDirectory_DeletesWithRetry_Success()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Act
                    await IOUtil.DeleteDirectoryWithRetry(tempDir, CancellationToken.None);

                    // Assert
                    Assert.False(Directory.Exists(tempDir));
                }
                finally
                {
                    // Cleanup
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async void DeleteDirectory_DeletesWithRetry_CancellationRequested()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "exclusiveFile.txt");
                //it blocks file inside using
                using (FileStream fs = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Act
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                        {
                            await IOUtil.DeleteDirectoryWithRetry(tempDir, cancellationTokenSource.Token);
                        });
                }

                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

            }
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void DeleteDirectory_DeletesWithRetry_NonExistenDir()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string nonExistentDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                // Act & Assert
                await IOUtil.DeleteDirectoryWithRetry(nonExistentDir, CancellationToken.None);

                // execution should not be thrown exception 

            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async void DeleteDirectory_DeletesWithRetry_IOException()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "exclusiveFile.txt");
                var exceptionThrown = false;
                //it blocks file inside using
                using (FileStream fs = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Act & Assert
                    try
                    {
                        await IOUtil.DeleteDirectoryWithRetry(tempDir, CancellationToken.None);
                    }
                    catch (AggregateException ae)
                    {
                        // Assert that at least one inner exception is an IOException
                        Assert.NotEmpty(ae.InnerExceptions.OfType<IOException>().ToList());
                        exceptionThrown = true;
                    }

                    finally
                    {
                        fs.Close();
                    }
                }
                Assert.True(exceptionThrown, "Exceptione should be thrown when trying to delete blocked file");
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesReadOnlyFiles()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");
                    File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }

                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DoesNotFollowDirectoryReparsePoint()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/linkDir -> targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir = Path.Combine(randomDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(linkDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DoesNotFollowNestLevel1DirectoryReparsePoint()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/subDir
                //   randomDir/subDir/linkDir -> ../targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string subDir = Directory.CreateDirectory(Path.Combine(randomDir, "subDir")).FullName;
                    string linkDir = Path.Combine(subDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(subDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(subDir));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DoesNotFollowNestLevel2DirectoryReparsePoint()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/subDir1
                //   randomDir/subDir1/subDir2
                //   randomDir/subDir1/subDir2/linkDir -> ../../targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string subDir1 = Directory.CreateDirectory(Path.Combine(randomDir, "subDir1")).FullName;
                    string subDir2 = Directory.CreateDirectory(Path.Combine(subDir1, "subDir2")).FullName;
                    string linkDir = Path.Combine(subDir2, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(subDir1, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(subDir1));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_IgnoresFile()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act: Call "DeleteDirectory" against the file. The method should not blow up and
                    // should simply ignore the file since it is not a directory.
                    IOUtil.DeleteDirectory(file, CancellationToken.None);

                    // Assert.
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteFile_DeletesFile()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.DeleteFile(file);

                    // Assert.
                    Assert.False(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteFile_DeletesReadOnlyFile()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");
                    File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

                    // Act.
                    IOUtil.DeleteFile(file);

                    // Assert.
                    Assert.False(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }

                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteFile_IgnoresDirectory()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(directory);

                    // Act: Call "DeleteFile" against a directory. The method should not blow up and
                    // should simply ignore the directory since it is not a file.
                    IOUtil.DeleteFile(directory);

                    // Assert.
                    Assert.True(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void DeleteFile_DeletesWithRetry_Success()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                string file = Path.Combine(tempDir, "some file");
                File.WriteAllText(path: file, contents: "some contents");
                try
                {
                    // Act
                    await IOUtil.DeleteFileWithRetry(file, CancellationToken.None);

                    // Assert
                    Assert.False(File.Exists(file));
                }
                finally
                {
                    // Cleanup
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async void DeleteFile_DeletesWithRetry_NonExistenFile()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string nonExistentFile = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                // Act & Assert
                await IOUtil.DeleteFileWithRetry(nonExistentFile, CancellationToken.None);

                // execution should not be thrown exception 

            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async void DeleteFile_DeletesWithRetry_IOException()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "exclusiveFile.txt");
                //it blocks file inside using
                using (FileStream fs = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await Assert.ThrowsAsync<IOException>(async () =>
                     {
                         await IOUtil.DeleteFileWithRetry(tempFile, CancellationToken.None);
                     });
                }
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async void DeleteFile_DeletesWithRetry_CancellationRequested()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                // Arrange
                string tempDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "exclusiveFile.txt");
                //it blocks file inside using
                using (FileStream fs = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                        {
                            await IOUtil.DeleteFileWithRetry(tempFile, cancellationTokenSource.Token);
                        });
                }
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }



        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public void GetRelativePathWindows()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string relativePath;
                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src") -> @"project\foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo.cpp", @"d:\src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\", @"d:\specs") -> @"d:\"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\", @"d:\specs");
                // Assert.
                Assert.True(string.Equals(relativePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj") -> @"d:\src\project\foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj");
                // Assert.
                Assert.True(string.Equals(relativePath, @"d:\src\project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo", @"d:\src") -> @"project\foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo", @"d:\src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\project\foo.cpp") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project", @"d:\src\project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo.cpp", @"d:/src") -> @"project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo.cpp", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo.cpp", @"d:\src") -> @"d:/src/project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo.cpp", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo", @"d:/src") -> @"project/foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d\src\project", @"d:/src/project") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project", @"d:/src/project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "windows")]
        public void GetRelativePathNonWindows()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string relativePath;
                /// MakeRelative(@"/user/src/project/foo.cpp", @"/user/src") -> @"project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo.cpp", @"/user/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project/foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user", @"/user/specs") -> @"/user"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user", @"/user/specs");
                // Assert.
                Assert.True(string.Equals(relativePath, @"/user", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project/foo.cpp", @"/user/src/proj") -> @"/user/src/project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo.cpp", @"/user/src/proj");
                // Assert.
                Assert.True(string.Equals(relativePath, @"/user/src/project/foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project/foo", @"/user/src") -> @"project/foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo", @"/user/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project/foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project", @"/user/src/project") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project", @"/user/src/project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public void ResolvePathWindows()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string resolvePath;
                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\", @"foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\", @"specs");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\specs", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\", @"src\proj");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project\src\proj", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\foo", @"..");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project", @"..\..\");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project", @"../.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project/", @"../../foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project/foo", @".././bar/.././../foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\", @".");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [Trait("SkipOn", "windows")]
        public void ResolvePathNonWindows()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string resolvePath;
                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src/project/foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/root", @"./user/./specs");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/root/user/specs", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/", @"user/specs/.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/specs", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"../");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"../../");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project/foo", @"../../../../user/./src");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src", @"../../.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/", @"./");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValidateExecutePermission_DoesNotExceedFailsafe()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(directory);

                    // Act/Assert: Call "ValidateExecutePermission". The method should not blow up.
                    IOUtil.ValidateExecutePermission(directory);
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValidateExecutePermission_ExceedsFailsafe()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a deep directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName(), "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20");
                try
                {
                    Directory.CreateDirectory(directory);
                    Environment.SetEnvironmentVariable("AGENT_TEST_VALIDATE_EXECUTE_PERMISSIONS_FAILSAFE", "20");

                    try
                    {
                        // Act: Call "ValidateExecutePermission". The method should throw since
                        // it exceeds the failsafe recursion depth.
                        IOUtil.ValidateExecutePermission(directory);

                        // Assert.
                        throw new Exception("Should have thrown not supported exception.");
                    }
                    catch (NotSupportedException)
                    {
                    }
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        private static async Task CreateDirectoryReparsePoint(IHostContext context, string link, string target)
        {
            string fileName = (TestUtil.IsWindows())
                ? Environment.GetEnvironmentVariable("ComSpec")
                : "/bin/ln";
            string arguments = (TestUtil.IsWindows())
                ? $@"/c ""mklink /J ""{link}"" {target}"""""
                : $@"-s ""{target}"" ""{link}""";

            ArgUtil.File(fileName, nameof(fileName));
            using (var processInvoker = new ProcessInvokerWrapper())
            {
                processInvoker.Initialize(context);
                await processInvoker.ExecuteAsync(
                    workingDirectory: context.GetDirectory(WellKnownDirectory.Bin),
                    fileName: fileName,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: true,
                    cancellationToken: CancellationToken.None);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:Prefer jagged arrays over multidimensional")]
        public void GetDirectoryName_LinuxStyle()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                string[,] testcases = new string[,] {
                    {"/foo/bar", "/foo"},
                    {"/foo", "/"},
                    {"/foo\\ bar/blah", "/foo\\ bar"}
                };

                for (int i = 0; i < testcases.GetLength(0); i++)
                {
                    var path = IOUtil.GetDirectoryName(testcases[i, 0], PlatformUtil.OS.Linux);
                    var expected = testcases[i, 1];
                    Assert.Equal(expected, path);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:Prefer jagged arrays over multidimensional")]
        public void GetDirectoryName_WindowsStyle()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                string[,] testcases = new string[,] {
                    {"c:\\foo\\bar", "c:\\foo"},
                    {"c:/foo/bar", "c:\\foo"}
                };

                for (int i = 0; i < testcases.GetLength(0); i++)
                {
                    var path = IOUtil.GetDirectoryName(testcases[i, 0], PlatformUtil.OS.Windows);
                    var expected = testcases[i, 1];
                    Assert.Equal(expected, path);
                }
            }
        }

        private static async Task CreateFileReparsePoint(IHostContext context, string link, string target)
        {
            string fileName = (TestUtil.IsWindows())
                ? Environment.GetEnvironmentVariable("ComSpec")
                : "/bin/ln";
            string arguments = (TestUtil.IsWindows())
                ? $@"/c ""mklink ""{link}"" ""{target}"""""
                : $@"-s ""{target}"" ""{link}""";

            ArgUtil.File(fileName, nameof(fileName));
            using (var processInvoker = new ProcessInvokerWrapper())
            {
                processInvoker.Initialize(context);
                await processInvoker.ExecuteAsync(
                    workingDirectory: context.GetDirectory(WellKnownDirectory.Bin),
                    fileName: fileName,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: true,
                    cancellationToken: CancellationToken.None);
            }
        }
    }
}
