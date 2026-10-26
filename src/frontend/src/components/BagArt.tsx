import { useId } from 'react';
import type { EarnedToolDto } from '../api/types';

type BagArtProps = {
  colour: string;
  worn?: boolean;
  label: string;
  tools: EarnedToolDto[];
  droppingSkillId?: string | null;
  /** Team-bag mode: percent filled per skill, drawn as a gauge in the patch socket. */
  fills?: Record<string, number>;
  style?: string;
};

/**
 * The bag, drawn as layered SVG so a colour change is a token change.
 * Empty bag for the walking skeleton; tools and patches land in later slices.
 */
export function BagArt({
  colour,
  worn = false,
  label,
  tools,
  droppingSkillId = null,
  fills,
  style = 'backpack',
}: BagArtProps) {
  const clipPrefix = useId().replace(/[^a-zA-Z0-9_-]/g, '');
  return (
    <svg
      className={`bag-art bag--${colour}${worn ? ' bag-art--worn' : ''} bag-art--style-${style}`}
      viewBox="0 0 460 540"
      role="img"
      aria-label={label}
    >
      {/* shadow */}
      <ellipse cx="230" cy="512" rx="138" ry="12" fill="var(--ink)" opacity="0.1" />

      {/* shoulder straps behind the pack */}
      <rect x="124" y="92" width="26" height="128" rx="13" fill="var(--bag-deep)" opacity="0.85" />
      <rect x="310" y="92" width="26" height="128" rx="13" fill="var(--bag-deep)" opacity="0.85" />

      {/* rolled mat strapped on top */}
      <g transform="translate(0 20)">
        <rect x="140" y="46" width="180" height="42" rx="21" fill="var(--rose)" />
        <path d="M158 52 C196 44 268 44 304 52" fill="none" stroke="var(--ink)" strokeOpacity="0.18" strokeWidth="2" strokeDasharray="5 7" />
        <rect x="174" y="42" width="14" height="50" rx="7" fill="var(--ink)" opacity="0.16" />
        <rect x="272" y="42" width="14" height="50" rx="7" fill="var(--ink)" opacity="0.16" />
      </g>

      {/* body */}
      <path
        d="M112 138 C110 128 118 120 128 120 L332 120 C342 120 350 128 348 138 L338 466 C337 482 326 494 310 494 L150 494 C134 494 123 482 122 466 Z"
        fill="var(--bag)"
        stroke="var(--bag-edge)"
        strokeWidth="3"
      />

      {/* opening — the empty inside */}
      <ellipse cx="230" cy="131" rx="104" ry="19" fill="#3a2f27" />
      <ellipse cx="230" cy="133" rx="95" ry="13" fill="#2a211b" opacity="0.85" />
      <path d="M126 128 C170 146 290 146 334 128" fill="none" stroke="var(--bag-edge)" strokeWidth="2.5" />

      {/* flap, folded forward */}
      <path
        d="M126 146 C176 164 284 164 334 146 L326 300 C282 318 178 318 134 300 Z"
        fill="var(--bag-light)"
        stroke="var(--bag-edge)"
        strokeWidth="2.5"
      />
      <path
        d="M142 162 C186 177 274 177 318 162 L311 286 C270 301 190 301 149 286 Z"
        fill="none"
        stroke="var(--ink)"
        strokeOpacity="0.22"
        strokeWidth="2"
        strokeDasharray="5 6"
      />

      {/* buckle and strap */}
      <rect x="212" y="250" width="36" height="94" rx="10" fill="var(--bag-deep)" />
      <path d="M220 258 L220 336 M240 258 L240 336" stroke="var(--ink)" strokeOpacity="0.18" strokeWidth="2" strokeDasharray="4 6" />
      <rect x="204" y="300" width="52" height="36" rx="7" fill="none" stroke="var(--brass)" strokeWidth="5" />
      <path d="M230 300 L230 328" stroke="var(--brass)" strokeWidth="4" strokeLinecap="round" />

      {/* side pockets */}
      <path
        d="M112 296 L84 308 C74 312 70 320 70 330 L70 414 C70 428 80 438 94 438 L118 438 Z"
        fill="var(--bag-deep)"
        stroke="var(--bag-edge)"
        strokeWidth="2.5"
      />
      <path
        d="M348 296 L376 308 C386 312 390 320 390 330 L390 414 C390 428 380 438 366 438 L342 438 Z"
        fill="var(--bag-deep)"
        stroke="var(--bag-edge)"
        strokeWidth="2.5"
      />
      <path d="M84 316 C88 330 92 344 92 362" fill="none" stroke="var(--ink)" strokeOpacity="0.2" strokeWidth="2" strokeDasharray="4 6" />
      <path d="M376 316 C372 330 368 344 368 362" fill="none" stroke="var(--ink)" strokeOpacity="0.2" strokeWidth="2" strokeDasharray="4 6" />

      {/* patch sockets — one per skill; earned skills are sewn in */}
      {Array.from({ length: 4 }, (_, index) => {
        const x = index % 2 === 0 ? 148 : 238;
        const y = index < 2 ? 356 : 414;
        const tool = tools[index];
        const tilt = index % 2 === 0 ? -1.5 : 1.5;
        const fill = tool ? fills?.[tool.skillId] : undefined;

        if (tool && fill !== undefined) {
          const percent = Math.max(0, Math.min(100, fill));

          return (
            <g key={tool.skillId} transform={`rotate(${tilt} ${x + 34} ${y + 23})`}>
              <rect
                x={x}
                y={y}
                width="68"
                height="46"
                rx="8"
                fill="var(--paper-raised)"
                stroke="var(--ink)"
                strokeOpacity="0.35"
                strokeWidth="2"
              />
              <clipPath id={`${clipPrefix}-${index}`}>
                <rect x={x} y={y} width="68" height="46" rx="8" />
              </clipPath>
              <g clipPath={`url(#${clipPrefix}-${index})`}>
                <rect
                  x={x}
                  y={y + 14 + 32 * (1 - percent / 100)}
                  width="68"
                  height={32 * (percent / 100)}
                  fill="var(--brass)"
                />
              </g>
              <rect x={x} y={y} width="68" height="14" fill="var(--paper-raised)" opacity="0.96" />
              <rect
                x={x + 5}
                y={y + 5}
                width="58"
                height="36"
                rx="5"
                fill="none"
                stroke="var(--ink)"
                strokeOpacity="0.3"
                strokeWidth="1.5"
                strokeDasharray="3 3"
              />
              <text
                x={x + 34}
                y={y + 10.5}
                textAnchor="middle"
                fontFamily="Inter, sans-serif"
                fontSize="8.5"
                fill="var(--ink)"
              >
                {tool.name.split(' ')[0]}
              </text>
            </g>
          );
        }

        return tool ? (
          <g key={tool.skillId} transform={`rotate(${tilt} ${x + 34} ${y + 23})`}>
            <g className={`patch${tool.skillId === droppingSkillId ? ' patch--drop' : ''}`}>
              <rect
                x={x}
                y={y}
                width="68"
                height="46"
                rx="8"
                fill={tool.honed ? 'var(--teal)' : 'var(--butter)'}
                stroke="var(--ink)"
                strokeOpacity="0.35"
                strokeWidth="2"
              />
              <rect
                x={x + 5}
                y={y + 5}
                width="58"
                height="36"
                rx="5"
                fill="none"
                stroke={tool.honed ? 'var(--paper)' : 'var(--ink)'}
                strokeOpacity="0.35"
                strokeWidth="1.5"
                strokeDasharray="3 3"
              />
              <text
                x={x + 34}
                y={tool.honed ? y + 21 : y + 24}
                textAnchor="middle"
                fontFamily="Inter, sans-serif"
                fontSize="9.5"
                fill={tool.honed ? 'var(--paper)' : 'var(--ink)'}
              >
                {tool.name.length > 9 ? (
                  <>
                    <tspan x={x + 34} dy="0">
                      {tool.name.split(' ')[0]}
                    </tspan>
                    <tspan x={x + 34} dy="10">
                      {tool.name.slice(tool.name.split(' ')[0].length + 1)}
                    </tspan>
                  </>
                ) : (
                  tool.name
                )}
              </text>
              {tool.honed && (
                <text
                  x={x + 34}
                  y={y + 37}
                  textAnchor="middle"
                  fontFamily="Inter, sans-serif"
                  fontSize="8"
                  fill="var(--paper)"
                  opacity="0.9"
                >
                  honed
                </text>
              )}
            </g>
          </g>
        ) : (
          <rect
            key={`empty-${index}`}
            x={x}
            y={y}
            width="68"
            height="46"
            rx="8"
            fill="none"
            stroke="var(--ink)"
            strokeOpacity="0.2"
            strokeWidth="2"
            strokeDasharray="4 6"
          />
        );
      })}

      {/* base stitch */}
      <path d="M136 476 L324 476" stroke="var(--ink)" strokeOpacity="0.18" strokeWidth="2" strokeDasharray="5 7" />

      {/* style cues */}
      <path
        className="bag-handle"
        d="M190 116 C202 90 258 90 270 116"
        fill="none"
        stroke="var(--bag-deep)"
        strokeWidth="9"
        strokeLinecap="round"
      />
      <path
        className="bag-sling-strap"
        d="M118 190 C220 250 280 330 344 446"
        fill="none"
        stroke="var(--bag-deep)"
        strokeWidth="14"
        strokeLinecap="round"
        opacity="0.9"
      />

      {/* carabiner */}
      <g stroke="var(--brass)" strokeWidth="3.5" fill="none">
        <circle cx="372" cy="226" r="11" />
        <path d="M361 226 L348 224" />
      </g>
    </svg>
  );
}
