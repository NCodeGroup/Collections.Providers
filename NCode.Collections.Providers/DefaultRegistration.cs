#region Copyright Preamble

// Copyright @ 2024 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NCode.Collections.Providers;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register the collection provider services.
/// </summary>
/// <remarks>
/// Use the <see cref="AddCollectionProviders"/> extension method to register all required services
/// for the collection provider infrastructure.
/// </remarks>
/// <seealso cref="ICollectionDataSourceFactory"/>
/// <seealso cref="ICollectionProviderFactory"/>
[PublicAPI]
public static class DefaultRegistration
{
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        /// Registers the required services for collection providers into the provided <see cref="IServiceCollection"/> instance.
        /// </summary>
        /// <returns>The <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the following services as singletons:
        /// </para>
        /// <list type="bullet">
        ///   <item><description><see cref="INullChangeToken"/> - The singleton <see cref="NullChangeToken.Singleton"/> instance</description></item>
        ///   <item><description><see cref="ICollectionDataSourceFactory"/> - <see cref="DefaultCollectionDataSourceFactory"/></description></item>
        ///   <item><description><see cref="ICollectionProviderFactory"/> - <see cref="DefaultCollectionProviderFactory"/></description></item>
        /// </list>
        /// <para>
        /// This method uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection)"/>
        /// so existing registrations will not be overwritten.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddCollectionProviders();
        /// </code>
        /// </example>
        [PublicAPI]
        public IServiceCollection AddCollectionProviders()
        {
            serviceCollection.TryAddSingleton<INullChangeToken>(NullChangeToken.Singleton);

            serviceCollection.TryAddSingleton<
                ICollectionDataSourceFactory,
                DefaultCollectionDataSourceFactory
            >();

            serviceCollection.TryAddSingleton<
                ICollectionProviderFactory,
                DefaultCollectionProviderFactory
            >();

            return serviceCollection;
        }
    }
}
