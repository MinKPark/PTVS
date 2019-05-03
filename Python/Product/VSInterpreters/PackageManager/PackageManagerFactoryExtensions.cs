﻿// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PythonTools.Analysis; // OK: ModulePath

namespace Microsoft.PythonTools.Interpreter {
    public static class PackageManagerFactoryExtensions {
        /// <summary>
        /// Determines whether the interpreter factory contains the specified
        /// modules.
        /// </summary>
        /// <returns>The names of the modules that were found.</returns>
        public static async Task<bool> HasModuleAsync(this IPythonInterpreterFactory factory, string moduleName, IInterpreterOptionsService interpreterOptions = null) {
            if (interpreterOptions != null) {
                foreach (var pm in interpreterOptions.GetPackageManagers(factory)) {
                    if ((await pm.GetInstalledPackageAsync(new PackageSpec(moduleName), CancellationToken.None)).IsValid) {
                        return true;
                    }
                }
            }

            return await Task.Run(() => {
                var configuration = factory.Configuration;
                var prefixPath = configuration.GetPrefixPath();
                var libraryPath = !string.IsNullOrEmpty(configuration.LibraryPath) ? configuration.LibraryPath : Path.Combine(prefixPath, "Lib");
                var sitePackagesPath = !string.IsNullOrEmpty(configuration.SitePackagesPath) ? configuration.SitePackagesPath : Path.Combine(libraryPath, "site-packages");
                var requiresInitPyFiles = ModulePath.PythonVersionRequiresInitPyFiles(configuration.Version);
                foreach (var mp in ModulePath.GetModulesInLib(libraryPath, sitePackagesPath, requiresInitPyFiles)) {
                    if (mp.ModuleName == moduleName) {
                        return true;
                    }
                }

                return false;
            });
        }
    }
}
