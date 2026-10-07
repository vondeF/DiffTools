using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Mal.Meta;
using Monitel.Serialization.CIMXML;

namespace sapphire_diffmaker.Entities
{
    public class DiffSettings
    {
        public int ModelNumberBefore { get; }
        public int ModelNumberAfter { get; }

        private bool _isMridExport = false;
        public bool IsMridExport
        {
            get => _isMridExport;
            set => _isMridExport = value;
        }
        private RdfProviderInfo _rdfProvider = null;
        public RdfProviderInfo RdfProvider
        {
            get => _rdfProvider;
            set => _rdfProvider = value;
        }
        private MetaProfile _profile = null;
        public MetaProfile Profile
        {
            get => _profile;
            set => _profile = value;
        }
        private List<string> _ignoredClasses = null;
        public List<string> IgnoredClasses
        {
            get => _ignoredClasses;
            set => _ignoredClasses = value;
        }

        public DiffSettings(int n1, int n2) 
        {
            ModelNumberBefore = n1;
            ModelNumberAfter = n2;
        }
    }
}
