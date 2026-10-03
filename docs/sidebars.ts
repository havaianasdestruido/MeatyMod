import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

/**
 * One hand-maintained sidebar so the ordering of the reference material is
 * explicit instead of alphabetical.
 */
const sidebars: SidebarsConfig = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting started',
      collapsed: false,
      items: [
        'getting-started/installation',
        'getting-started/quickstart',
        'getting-started/repository-layout',
      ],
    },
    {
      type: 'category',
      label: 'Architecture',
      collapsed: false,
      items: [
        'architecture/overview',
        'architecture/injection-pipeline',
        'architecture/mod-runtime',
        'architecture/backup-and-restore',
        'architecture/security-model',
      ],
    },
    {
      type: 'category',
      label: 'CLI reference',
      collapsed: true,
      items: [
        'cli/overview',
        'cli/pack',
        'cli/install',
        'cli/inject',
        'cli/restore',
        'cli/manifest',
        'cli/verify',
        'cli/parse',
        'cli/xnb',
        'cli/checksum',
      ],
    },
    {
      type: 'category',
      label: 'API reference',
      collapsed: true,
      items: [
        'api/overview',
        'api/meatymod-core',
        'api/meatymod-formats',
        'api/meatymod-injector',
        'api/meatymod-assets',
        'api/meatymod-verifier',
        'api/meatymod-cli',
      ],
    },
    {
      type: 'category',
      label: 'File formats',
      collapsed: true,
      items: [
        'formats/overview',
        'formats/xnb',
        'formats/txt',
        'formats/raw',
        'formats/mod-manifest',
        'formats/mod-package',
        'formats/mod-config',
      ],
    },
    {
      type: 'category',
      label: 'Mods',
      collapsed: true,
      items: [
        'mods/overview',
        'mods/quackmenu',
        'mods/oink',
        'mods/authoring-a-mod',
      ],
    },
    {
      type: 'category',
      label: 'Tools & scripts',
      collapsed: true,
      items: ['tools/overview', 'tools/modharness', 'tools/memscan'],
    },
    {
      type: 'category',
      label: 'Development',
      collapsed: true,
      items: [
        'development/building',
        'development/testing',
        'development/coding-standards',
        'development/documentation-site',
        'development/contributing',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      collapsed: true,
      items: [
        'reference/troubleshooting',
        'reference/faq',
        'reference/glossary',
        'reference/third-party',
      ],
    },
  ],
};

export default sidebars;
