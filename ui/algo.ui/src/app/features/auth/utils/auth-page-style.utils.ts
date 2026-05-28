import { AdminAuthPageDesignConfig } from '../../../core/config/admin-template-config.model';

export function authPageBackground(design: AdminAuthPageDesignConfig): string {
  return [
    `radial-gradient(circle at top left, ${hexToRgba(design.accentColor, design.accentOpacity)}, transparent ${design.accentSizePercent}%)`,
    `linear-gradient(180deg, ${design.backgroundStart}, ${design.backgroundEnd})`
  ].join(',');
}

export function authCardStyle(
  design: AdminAuthPageDesignConfig,
  widthRem: number
): Record<string, string> {
  return {
    maxWidth: `${widthRem}rem`,
    background: hexToRgba(design.cardBackground, 0.92),
    borderColor: hexToRgba(design.cardBorderColor, 0.7),
    borderRadius: `${design.cardRadiusPx}px`,
    boxShadow: design.cardShadow
  };
}

export function authButtonStyle(design: AdminAuthPageDesignConfig): Record<string, string> {
  return {
    background: design.buttonBackground,
    borderColor: design.buttonBackground,
    color: design.buttonTextColor
  };
}

function hexToRgba(hex: string, alpha: number): string {
  const normalized = hex.replace('#', '').trim();
  const full = normalized.length === 3
    ? normalized.split('').map((char) => `${char}${char}`).join('')
    : normalized;

  if (full.length !== 6) {
    return hex;
  }

  const red = Number.parseInt(full.slice(0, 2), 16);
  const green = Number.parseInt(full.slice(2, 4), 16);
  const blue = Number.parseInt(full.slice(4, 6), 16);

  return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}
