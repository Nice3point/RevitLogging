using System.Reflection;
using System.Runtime.CompilerServices;
using Autodesk.Revit.ApplicationServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Nice3point.Revit.Logging.Formatters;
using Nice3point.Revit.Logging.Writers;

namespace Nice3point.Revit.Logging;

/// <summary>
///     Provides extension methods for the <see cref="ILoggingBuilder" /> class.
/// </summary>
[PublicAPI]
public static class RevitLoggingRegistration
{
    /// <param name="logging">The <see cref="ILoggingBuilder" /> to use.</param>
    extension(ILoggingBuilder logging)
    {
        /// <summary>
        ///     Adds a journal logger named 'RevitJournal' to the factory.
        /// </summary>
        /// <param name="application">The application of the running Revit session.</param>
        /// <param name="configure">A delegate to configure the <see cref="RevitJournalLoggerOptions" />.</param>
        /// <returns>The <see cref="ILoggingBuilder" /> for chaining.</returns>
        /// <remarks>The delegate runs after the configuration is bound and overrides it.</remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ILoggingBuilder AddRevitJournal(Application application, Action<RevitJournalLoggerOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(application);

            return AddJournalProvider(logging, new ApplicationJournalWriter(application), Assembly.GetCallingAssembly(), configure);
        }

        /// <summary>
        ///     Adds a journal logger named 'RevitJournal' to the factory.
        /// </summary>
        /// <param name="application">The controlled application of the running Revit session.</param>
        /// <param name="configure">A delegate to configure the <see cref="RevitJournalLoggerOptions" />.</param>
        /// <returns>The <see cref="ILoggingBuilder" /> for chaining.</returns>
        /// <remarks>The delegate runs after the configuration is bound and overrides it.</remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ILoggingBuilder AddRevitJournal(ControlledApplication application, Action<RevitJournalLoggerOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(application);

            return AddJournalProvider(logging, new ControlledApplicationJournalWriter(application), Assembly.GetCallingAssembly(), configure);
        }

        /// <summary>
        ///     Sets the formatter the journal logger renders every record with.
        /// </summary>
        /// <typeparam name="TFormatter">The type of the formatter to render every record with.</typeparam>
        /// <returns>The <see cref="ILoggingBuilder" /> for chaining.</returns>
        public ILoggingBuilder AddRevitJournalFormatter<TFormatter>() where TFormatter : RevitJournalFormatter
        {
            logging.Services.Replace(ServiceDescriptor.Singleton<RevitJournalFormatter, TFormatter>());

            return logging;
        }
    }

    private static ILoggingBuilder AddJournalProvider(
        ILoggingBuilder logging,
        IJournalWriter writer,
        Assembly callingAssembly,
        Action<RevitJournalLoggerOptions>? configure)
    {
        logging.AddConfiguration();

        var applicationName = callingAssembly.GetName().Name;
        if (applicationName is not null)
        {
            logging.Services.Configure<RevitJournalLoggerOptions>(options => options.ApplicationName = applicationName);
        }

        LoggerProviderOptions.RegisterProviderOptions<RevitJournalLoggerOptions, RevitJournalLoggerProvider>(logging.Services);

        logging.Services.TryAddSingleton(writer);
        logging.Services.TryAddSingleton<RevitJournalFormatter, DefaultRevitJournalFormatter>();
        logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RevitJournalLoggerProvider>());

        if (configure is not null)
        {
            logging.Services.Configure(configure);
        }

        return logging;
    }
}
