import argparse
import os
import subprocess
import tarfile
import zipfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("version", help="The version tag, starts with v")
    parser.add_argument(
        "runtime",
        help="The runtime environment, e.g. win-x64, linux-x64, osx-x64, linux-arm64, osx-arm64",
    )
    parser.add_argument(
        "--enable-aot", action="store_true", help="Enable AOT compilation"
    )
    args = parser.parse_args()

    version = args.version.lstrip("v")
    project_name = "notation-azure-kv"
    output_dir = os.path.join(".", "bin", "publish")
    os.makedirs(output_dir, exist_ok=True)
    artifacts_dir = os.path.join(".", "bin", "artifacts")
    os.makedirs(artifacts_dir, exist_ok=True)

    # Get the latest commit hash
    commit_hash = subprocess.check_output(
        ["git", "log", "--pretty=format:%h", "-n", "1"]
    ).decode("utf-8")
    print(f"Commit hash: {commit_hash}")

    # Prepare the dotnet publish command
    publish_command = [
        "dotnet",
        "publish",
        "./Notation.Plugin.AzureKeyVault",
        "--configuration",
        "Release",
        "--self-contained",
        "true",
        f"-p:CommitHash={commit_hash}",
        f"-p:Version={version}",
        "-r",
        args.runtime,
        "-o",
        os.path.join(output_dir, args.runtime),
    ]

    if args.enable_aot:
        publish_command.append("-p:PublishAot=true")
    else:
        publish_command.append("-p:PublishSingleFile=true")

    # Publish for each runtime
    subprocess.run(publish_command, check=True)

    # Determine the target platform
    if args.runtime.startswith("win"):
        ext = "zip"
        binary_name = f"{project_name}.exe"
    else:
        ext = "tar.gz"
        binary_name = project_name

    # Apply the runtime name mapping
    mapped_runtime = args.runtime.replace("x64", "amd64")
    mapped_runtime = mapped_runtime.replace("win", "windows")
    mapped_runtime = mapped_runtime.replace("osx", "darwin")
    mapped_runtime = mapped_runtime.replace("-", "_")
    artifact_name = f"{artifacts_dir}/{project_name}_{version}_{mapped_runtime}.{ext}"
    binary_dir = f"{output_dir}/{args.runtime}"

    # Create the artifact
    if ext == "zip":
        with zipfile.ZipFile(artifact_name, "w", zipfile.ZIP_DEFLATED) as zipf:
            zipf.write(os.path.join(binary_dir, binary_name), arcname=binary_name)
            zipf.write("LICENSE")
    else:
        with tarfile.open(artifact_name, "w:gz") as tar:
            tar.add(os.path.join(binary_dir, binary_name), arcname=binary_name)
            tar.add("LICENSE")


if __name__ == "__main__":
    main()
