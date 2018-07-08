// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// https://github.com/Microsoft/msbuild/blob/d136a42365dba0e6fe5f96dbd0ee4ba6ca77aa8b/src/Tasks/Unzip.cs

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Xamarin.Android.Lite.Bootstrap
{
	/// <summary>
	/// Represents a task that can extract a .zip archive.
	/// </summary>
	public sealed class Unzip : Task, ICancelableTask
	{
		// We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
		// The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
		// improvement in Copy performance.
		private const int _DefaultCopyBufferSize = 81920;

		/// <summary>
		/// Stores a <see cref="CancellationTokenSource"/> used for cancellation.
		/// </summary>
		private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource ();

		/// <summary>
		/// Gets or sets a <see cref="ITaskItem"/> with a destination folder path to unzip the files to.
		/// </summary>
		[Required]
		public ITaskItem DestinationFolder { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether read-only files should be overwritten.
		/// </summary>
		public bool OverwriteReadOnlyFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether files should be skipped if the destination is unchanged.
		/// </summary>
		public bool SkipUnchangedFiles { get; set; } = true;

		/// <summary>
		/// In the case of some zips, they have a top directory named the same as the zip file...
		/// </summary>
		public bool SkipTopDirectory { get; set; }

		/// <summary>
		/// Gets or sets an array of <see cref="ITaskItem"/> objects containing the paths to .zip archive files to unzip.
		/// </summary>
		[Required]
		public ITaskItem [] SourceFiles { get; set; }

		/// <inheritdoc cref="ICancelableTask.Cancel"/>
		public void Cancel ()
		{
			_cancellationToken.Cancel ();
		}

		/// <inheritdoc cref="Task.Execute"/>
		public override bool Execute ()
		{
			DirectoryInfo destinationDirectory;
			try {
				destinationDirectory = Directory.CreateDirectory (DestinationFolder.ItemSpec);
			} catch (Exception e) {
				Log.LogError ("MSB3931: Failed to unzip to directory \"{0}\" because it could not be created.  {1}", DestinationFolder.ItemSpec, e.Message);

				return false;
			}

			BuildEngine3.Yield ();

			try {
				foreach (ITaskItem sourceFile in SourceFiles.TakeWhile (i => !_cancellationToken.IsCancellationRequested)) {
					if (!File.Exists (sourceFile.ItemSpec)) {
						Log.LogError ("MSB3932: Failed to unzip file \"{0}\" because the file does not exist or is inaccessible.", sourceFile.ItemSpec);
						continue;
					}

					try {
						using (FileStream stream = new FileStream (sourceFile.ItemSpec, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false)) {
							using (ZipArchive zipArchive = new ZipArchive (stream, ZipArchiveMode.Read, leaveOpen: false)) {
								try {
									Extract (zipArchive, destinationDirectory);
								} catch (Exception e) {
									// Unhandled exception in Extract() is a bug!
									Log.LogErrorFromException (e, showStackTrace: true);
									return false;
								}
							}
						}
					} catch (OperationCanceledException) {
						break;
					} catch (Exception e) {
						// Should only be thrown if the archive could not be opened (Access denied, corrupt file, etc)
						Log.LogError ("MSB3933: Failed to open zip file \"{0}\".  {1}", sourceFile.ItemSpec, e.Message);
					}
				}
			} finally {
				BuildEngine3.Reacquire ();
			}

			return !_cancellationToken.IsCancellationRequested && !Log.HasLoggedErrors;
		}

		/// <summary>
		/// Extracts all files to the specified directory.
		/// </summary>
		/// <param name="sourceArchive">The <see cref="ZipArchive"/> containing the files to extract.</param>
		/// <param name="destinationDirectory">The <see cref="DirectoryInfo"/> to extract files to.</param>
		private void Extract (ZipArchive sourceArchive, DirectoryInfo destinationDirectory)
		{
			foreach (ZipArchiveEntry zipArchiveEntry in sourceArchive.Entries.TakeWhile (i => !_cancellationToken.IsCancellationRequested)) {
				//entry.FullName can have / or \ depending on your .NET version
				var entryPath = zipArchiveEntry.FullName.Replace ('/', Path.DirectorySeparatorChar);
				if (SkipTopDirectory) {
					entryPath = entryPath.Substring (entryPath.IndexOf (Path.DirectorySeparatorChar) + 1);
				}
				//Skip directory entries
				if (entryPath.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
					Log.LogMessage (MessageImportance.Low, "Skipping directory entry: {0}", zipArchiveEntry.FullName);
					continue;
				}

				var destinationPath = new FileInfo (Path.Combine (destinationDirectory.FullName, entryPath));

				if (!destinationPath.FullName.StartsWith (destinationDirectory.FullName, StringComparison.OrdinalIgnoreCase)) {
					// ExtractToDirectory() throws an IOException for this but since we're extracting one file at a time
					// for logging and cancellation, we need to check for it ourselves.
					Log.LogError ("MSB3934: Failed to open unzip file \"{0}\" to \"{1}\" because it is outside the destination directory.", destinationPath.FullName, destinationDirectory.FullName);
					continue;
				}

				if (ShouldSkipEntry (zipArchiveEntry, destinationPath)) {
					Log.LogMessage (MessageImportance.Low, "Did not unzip from file \"{0}\" to file \"{1}\" because the \"{2}\" parameter was set to \"{3}\" in the project and the files' sizes and timestamps match.", zipArchiveEntry.FullName, destinationPath.FullName, nameof (SkipUnchangedFiles), "true");
					continue;
				}

				try {
					destinationPath.Directory?.Create ();
				} catch (Exception e) {
					Log.LogError ("MSB3931: Failed to unzip to directory \"{0}\" because it could not be created.  {1}", destinationPath.DirectoryName, e.Message);
					continue;
				}

				if (OverwriteReadOnlyFiles && destinationPath.Exists && destinationPath.IsReadOnly) {
					try {
						destinationPath.IsReadOnly = false;
					} catch (Exception e) {
						Log.LogError ("MSB3935: Failed to unzip file \"{0}\" because destination file \"{1}\" is read-only and could not be made writable.  {2}", zipArchiveEntry.FullName, destinationPath.FullName, e.Message);
						continue;
					}
				}

				try {
					Log.LogMessage (MessageImportance.Normal, "Copying file from \"{0}\" to \"{1}\".", zipArchiveEntry.FullName, destinationPath.FullName);

					using (Stream destination = File.Open (destinationPath.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
					using (Stream stream = zipArchiveEntry.Open ()) {
						stream.CopyToAsync (destination, _DefaultCopyBufferSize, _cancellationToken.Token)
							.ConfigureAwait (continueOnCapturedContext: false)
							.GetAwaiter ()
							.GetResult ();
					}

					destinationPath.LastWriteTimeUtc = zipArchiveEntry.LastWriteTime.UtcDateTime;
				} catch (IOException e) {
					Log.LogError ("MSB3936: Failed to open unzip file \"{0}\" to \"{1}\".  {2}", zipArchiveEntry.FullName, destinationPath.FullName, e.Message);
				}
			}
		}

		/// <summary>
		/// Determines whether or not a file should be skipped when unzipping.
		/// </summary>
		/// <param name="zipArchiveEntry">The <see cref="ZipArchiveEntry"/> object containing information about the file in the zip archive.</param>
		/// <param name="fileInfo">A <see cref="FileInfo"/> object containing information about the destination file.</param>
		/// <returns><code>true</code> if the file should be skipped, otherwise <code>false</code>.</returns>
		private bool ShouldSkipEntry (ZipArchiveEntry zipArchiveEntry, FileInfo fileInfo)
		{
			return SkipUnchangedFiles
				   && fileInfo.Exists
				   && zipArchiveEntry.LastWriteTime == fileInfo.LastWriteTimeUtc
				   && zipArchiveEntry.Length == fileInfo.Length;
		}
	}
}