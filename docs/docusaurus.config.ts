import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const organizationName = 'havaianasdestruido';
const projectName = 'MeatyMod';

/**
 * The marketing site (Jekyll) owns `/MeatyMod/`.
 * This Docusaurus instance owns `/MeatyMod/docs/` and nothing else.
 */
const siteUrl = `https://${organizationName}.github.io`;
const siteBaseUrl = `/${projectName}/`;
const docsBaseUrl = `${siteBaseUrl}docs/`;

// Fully-qualified link back to the Jekyll landing page. It must be absolute:
// a root-relative path would be rewritten against this sub-site's baseUrl.
const mainSiteUrl = `${siteUrl}${siteBaseUrl}`;

const config: Config = {
  title: 'MeatyMod Docs',
  tagline: 'Pack, inject, parse, verify and restore Blood & Bacon mods.',
  favicon: 'img/favicon.png',

  future: {
    v4: true,
  },

  url: siteUrl,
  baseUrl: docsBaseUrl,

  organizationName,
  projectName,
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'warn',
  onDuplicateRoutes: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  // Web fonts are requested from the document head rather than with an
  // `@import` inside custom.css: an `@import` is only discovered after the
  // stylesheet has been fetched and parsed, which serialises the requests and
  // delays first paint. `preconnect` warms the connection in parallel.
  // The matching markup lives in site/_includes/head.html.
  headTags: [
    {
      tagName: 'link',
      attributes: {rel: 'preconnect', href: 'https://fonts.googleapis.com'},
    },
    {
      tagName: 'link',
      attributes: {rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: 'anonymous'},
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'stylesheet',
        href: 'https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&family=Inter:wght@400;500;600;700;800&display=swap',
      },
    },
  ],

  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  themes: ['@docusaurus/theme-mermaid'],

  presets: [
    [
      'classic',
      {
        docs: {
          // Docs are the whole point of this sub-site, so they live at its root:
          // https://havaianasdestruido.github.io/MeatyMod/docs/
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
          editUrl: `https://github.com/${organizationName}/${projectName}/edit/main/docs/`,
          showLastUpdateTime: true,
          breadcrumbs: true,
        },
        // The landing page is served by Jekyll at /MeatyMod/ — no blog here.
        blog: false,
        pages: false,
        theme: {
          customCss: './src/css/custom.css',
        },
        sitemap: {
          lastmod: 'date',
          changefreq: 'weekly',
          priority: 0.5,
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/social-card.png',
    metadata: [
      {
        name: 'description',
        content:
          'Full technical documentation for MeatyMod, the C# mod suite and IL injector for Blood & Bacon.',
      },
    ],
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: false,
    },
    docs: {
      sidebar: {
        hideable: true,
        autoCollapseCategories: false,
      },
    },
    navbar: {
      title: 'MeatyMod',
      logo: {
        alt: 'MeatyMod logo',
        src: 'img/logo.png',
        href: mainSiteUrl,
        target: '_self',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Documentation',
        },
        {to: '/cli/overview', label: 'CLI', position: 'left'},
        {to: '/api/overview', label: 'API', position: 'left'},
        {to: '/mods/overview', label: 'Mods', position: 'left'},
        {
          href: mainSiteUrl,
          label: 'Main site',
          position: 'right',
          target: '_self',
        },
        {
          href: `https://github.com/${organizationName}/${projectName}`,
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Start here',
          items: [
            {label: 'Introduction', to: '/'},
            {label: 'Installation', to: '/getting-started/installation'},
            {label: 'Quickstart', to: '/getting-started/quickstart'},
            {label: 'Troubleshooting', to: '/reference/troubleshooting'},
          ],
        },
        {
          title: 'Reference',
          items: [
            {label: 'CLI commands', to: '/cli/overview'},
            {label: 'API reference', to: '/api/overview'},
            {label: 'File formats', to: '/formats/overview'},
            {label: 'Glossary', to: '/reference/glossary'},
          ],
        },
        {
          title: 'More',
          items: [
            {label: 'Main site', href: mainSiteUrl},
            {
              label: 'GitHub',
              href: `https://github.com/${organizationName}/${projectName}`,
            },
            {
              label: 'Issues',
              href: `https://github.com/${organizationName}/${projectName}/issues`,
            },
          ],
        },
      ],
      copyright: `MeatyMod — community modding tools for Blood & Bacon. Docs built with Docusaurus. © ${new Date().getFullYear()}.`,
    },
    prism: {
      theme: prismThemes.vsLight,
      darkTheme: prismThemes.vsDark,
      additionalLanguages: [
        'csharp',
        'powershell',
        'batch',
        'ini',
        'json',
        'bash',
        'diff',
      ],
    },
    tableOfContents: {
      minHeadingLevel: 2,
      maxHeadingLevel: 4,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
