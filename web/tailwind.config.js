/** @type {import('tailwindcss').Config} */

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        sg: {
          primary: {
            DEFAULT: 'var(--sg-primary)',
            hover: 'var(--sg-primary-hover)',
            soft: 'var(--sg-primary-soft)',
          },
          secondary: {
            DEFAULT: 'var(--sg-secondary)',
            hover: 'var(--sg-secondary-hover)',
            soft: 'var(--sg-secondary-soft)',
          },
          accent: {
            DEFAULT: 'var(--sg-accent)',
            soft: 'var(--sg-accent-soft)',
          },
          cyan: {
            DEFAULT: 'var(--sg-cyan)',
            soft: 'var(--sg-cyan-soft)',
          },
          success: {
            DEFAULT: 'var(--sg-success)',
            hover: 'var(--sg-success-hover)',
            soft: 'var(--sg-success-soft)',
            text: 'var(--sg-success-text)',
          },
          warning: {
            DEFAULT: 'var(--sg-warning)',
            hover: 'var(--sg-warning-hover)',
            soft: 'var(--sg-warning-soft)',
            text: 'var(--sg-warning-text)',
          },
          danger: {
            DEFAULT: 'var(--sg-danger)',
            hover: 'var(--sg-danger-hover)',
            soft: 'var(--sg-danger-soft)',
            text: 'var(--sg-danger-text)',
          },
          info: {
            DEFAULT: 'var(--sg-info)',
            hover: 'var(--sg-info-hover)',
            soft: 'var(--sg-info-soft)',
            text: 'var(--sg-info-text)',
          },
          bg: 'var(--sg-bg)',
          surface: {
            DEFAULT: 'var(--sg-surface)',
            muted: 'var(--sg-surface-muted)',
            hover: 'var(--sg-surface-hover)',
          },
          border: {
            DEFAULT: 'var(--sg-border)',
            strong: 'var(--sg-border-strong)',
            focus: 'var(--sg-border-focus)',
          },
          text: {
            DEFAULT: 'var(--sg-text)',
            secondary: 'var(--sg-text-secondary)',
            muted: 'var(--sg-text-muted)',
            disabled: 'var(--sg-text-disabled)',
            'on-primary': 'var(--sg-text-on-primary)',
          },
          white: 'var(--sg-white)',
          black: 'var(--sg-black)',
        },
      },

      // Keys match base.css exactly. The numeric values are identical to
      // Tailwind's defaults, so p-4 still renders 16px - it just reads the
      // token now instead of a hardcoded rem.
      spacing: {
        1: 'var(--sg-space-1)',
        2: 'var(--sg-space-2)',
        3: 'var(--sg-space-3)',
        4: 'var(--sg-space-4)',
        5: 'var(--sg-space-5)',
        6: 'var(--sg-space-6)',
        8: 'var(--sg-space-8)',
        10: 'var(--sg-space-10)',
        12: 'var(--sg-space-12)',
        16: 'var(--sg-space-16)',
      },

      borderRadius: {
        sm: 'var(--sg-radius-sm)',
        md: 'var(--sg-radius-md)',
        lg: 'var(--sg-radius-lg)',
        xl: 'var(--sg-radius-xl)',
        full: 'var(--sg-radius-full)',
      },

      boxShadow: {
        sm: 'var(--sg-shadow-sm)',
        md: 'var(--sg-shadow-md)',
        lg: 'var(--sg-shadow-lg)',
      },

      fontFamily: {
        sans: 'var(--sg-font-sans)',
        mono: 'var(--sg-font-mono)',
      },

      fontSize: {
        xs: 'var(--sg-text-xs)',
        sm: 'var(--sg-text-sm)',
        base: 'var(--sg-text-md)',
        lg: 'var(--sg-text-lg)',
        xl: 'var(--sg-text-xl)',
        '2xl': 'var(--sg-text-2xl)',
        '3xl': 'var(--sg-text-3xl)',
        '4xl': 'var(--sg-text-4xl)',
        '5xl': 'var(--sg-text-5xl)',
      },

      fontWeight: {
        normal: 'var(--sg-weight-regular)',
        medium: 'var(--sg-weight-medium)',
        semibold: 'var(--sg-weight-semibold)',
        bold: 'var(--sg-weight-bold)',
      },

      lineHeight: {
        tight: 'var(--sg-leading-tight)',
        normal: 'var(--sg-leading-normal)',
        relaxed: 'var(--sg-leading-relaxed)',
      },

      transitionDuration: {
        fast: 'var(--sg-duration-fast)',
        normal: 'var(--sg-duration-normal)',
        slow: 'var(--sg-duration-slow)',
      },

      transitionTimingFunction: {
        sg: 'var(--sg-ease)',
      },

      zIndex: {
        dropdown: 'var(--sg-z-dropdown)',
        sticky: 'var(--sg-z-sticky)',
        modal: 'var(--sg-z-modal)',
        toast: 'var(--sg-z-toast)',
      },

      maxWidth: {
        content: 'var(--sg-content-max-width)',
      },

      width: {
        sidebar: 'var(--sg-sidebar-width)',
      },

      height: {
        header: 'var(--sg-header-height)',
      },
    },
  },
  plugins: [],
}
