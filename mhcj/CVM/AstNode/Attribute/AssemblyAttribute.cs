using System;

namespace Microsoft.CodeAnalysis.CSharp.AstNode
{
    internal class AssemblyAttribute: A_Attribute
    {
        public static readonly string StringMissingValue = nameof(StringMissingValue);
        #region AssemblySignatureKeyAttributeSetting
        private string _assemblySignatureKeyAttributeSetting;
        public string AssemblySignatureKeyAttributeSetting
        {
            get
            {

                return _assemblySignatureKeyAttributeSetting;
            }
            set
            {

                _assemblySignatureKeyAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyDelaySignAttributeSetting
        private byte _assemblyDelaySignAttributeSetting;
        public byte AssemblyDelaySignAttributeSetting
        {
            get
            {

                return _assemblyDelaySignAttributeSetting;
            }
            set
            {

                _assemblyDelaySignAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyKeyFileAttributeSetting
        private string _assemblyKeyFileAttributeSetting = StringMissingValue;
        public string AssemblyKeyFileAttributeSetting
        {
            get
            {

                return _assemblyKeyFileAttributeSetting;
            }
            set
            {

                _assemblyKeyFileAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyKeyContainerAttributeSetting
        private string _assemblyKeyContainerAttributeSetting = StringMissingValue;
        public string AssemblyKeyContainerAttributeSetting
        {
            get
            {

                return _assemblyKeyContainerAttributeSetting;
            }
            set
            {

                _assemblyKeyContainerAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyVersionAttributeSetting
        private Version _assemblyVersionAttributeSetting;

        /// <summary>
        /// Raw assembly version as specified in the AssemblyVersionAttribute, or Nothing if none specified.
        /// If the string passed to AssemblyVersionAttribute contains * the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </summary>
        public Version AssemblyVersionAttributeSetting
        {
            get
            {

                return _assemblyVersionAttributeSetting;
            }
            set
            {

                _assemblyVersionAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyFileVersionAttributeSetting
        private string _assemblyFileVersionAttributeSetting;
        public string AssemblyFileVersionAttributeSetting
        {
            get
            {

                return _assemblyFileVersionAttributeSetting;
            }
            set
            {

                _assemblyFileVersionAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyTitleAttributeSetting
        private string _assemblyTitleAttributeSetting;
        public string AssemblyTitleAttributeSetting
        {
            get
            {

                return _assemblyTitleAttributeSetting;
            }
            set
            {

                _assemblyTitleAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyDescriptionAttributeSetting
        private string _assemblyDescriptionAttributeSetting;
        public string AssemblyDescriptionAttributeSetting
        {
            get
            {

                return _assemblyDescriptionAttributeSetting;
            }
            set
            {

                _assemblyDescriptionAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyCultureAttributeSetting
        private string _assemblyCultureAttributeSetting;
        public string AssemblyCultureAttributeSetting
        {
            get
            {

                return _assemblyCultureAttributeSetting;
            }
            set
            {

                _assemblyCultureAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyCompanyAttributeSetting
        private string _assemblyCompanyAttributeSetting;
        public string AssemblyCompanyAttributeSetting
        {
            get
            {

                return _assemblyCompanyAttributeSetting;
            }
            set
            {

                _assemblyCompanyAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyProductAttributeSetting
        private string _assemblyProductAttributeSetting;
        public string AssemblyProductAttributeSetting
        {
            get
            {

                return _assemblyProductAttributeSetting;
            }
            set
            {

                _assemblyProductAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyInformationalVersionAttributeSetting
        private string _assemblyInformationalVersionAttributeSetting;
        public string AssemblyInformationalVersionAttributeSetting
        {
            get
            {

                return _assemblyInformationalVersionAttributeSetting;
            }
            set
            {

                _assemblyInformationalVersionAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyCopyrightAttributeSetting
        private string _assemblyCopyrightAttributeSetting;
        public string AssemblyCopyrightAttributeSetting
        {
            get
            {

                return _assemblyCopyrightAttributeSetting;
            }
            set
            {

                _assemblyCopyrightAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyTrademarkAttributeSetting
        private string _assemblyTrademarkAttributeSetting;
        public string AssemblyTrademarkAttributeSetting
        {
            get
            {

                return _assemblyTrademarkAttributeSetting;
            }
            set
            {

                _assemblyTrademarkAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyFlagsAttributeSetting
        private int _assemblyFlagsAttributeSetting;
        public int AssemblyFlagsAttributeSetting
        {
            get
            {

                return _assemblyFlagsAttributeSetting;
            }
            set
            {

                _assemblyFlagsAttributeSetting = value;

            }
        }
        #endregion

        #region AssemblyAlgorithmIdAttribute
        private int? _assemblyAlgorithmIdAttributeSetting;
        public int? AssemblyAlgorithmIdAttributeSetting
        {
            get
            {

                return _assemblyAlgorithmIdAttributeSetting;
            }
            set
            {

                _assemblyAlgorithmIdAttributeSetting = value;

            }
        }
        #endregion

        internal AssemblyAttribute(BoundKind kind, SyntaxNode node, bool error = false) : base(kind, node)
        {

        }
    }
}
