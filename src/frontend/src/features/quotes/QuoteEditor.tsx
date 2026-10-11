import { useEffect, useMemo, useState } from "react";

import type {
  IncotermDto,
  Product,
  QuoteDto,
  QuoteRevisionInput,
  User,
} from "../../shared/api/contracts";
import { QuoteApprovalPanel } from "./QuoteApprovalPanel";
import { QuoteCommercialTerms } from "./QuoteCommercialTerms";
import {
  calculateQuote,
  calculateQuoteLineAmount,
  calculateQuoteSubtotal,
  validateQuoteDraft,
} from "./quoteCalculations";

export function QuoteEditor({
  quote,
  user,
  incoterms,
  products,
  busy,
  onSave,
  onSubmit,
  onApprove,
  onReject,
  onNewRevision,
  onPdf,
  onDispatch,
  onResult,
}: {
  quote: QuoteDto;
  user: User;
  incoterms: IncotermDto[];
  products: Product[];
  busy: boolean;
  onSave: (input: QuoteRevisionInput) => void;
  onSubmit: () => void;
  onApprove: () => void;
  onReject: () => void;
  onNewRevision: () => void;
  onPdf: (language: "ko" | "en") => void;
  onDispatch: () => void;
  onResult: (result: "won" | "lost" | "onHold") => void;
}) {
  const revision = quote.currentRevision;
  const editable = ["draft", "rejected"].includes(revision.status);
  const [draft, setDraft] = useState<QuoteRevisionInput>(() =>
    toInput(revision),
  );
  useEffect(() => setDraft(toInput(revision)), [revision]);
  const subtotalAmount = calculateQuoteSubtotal(draft.currency, draft.lines);
  const totals = useMemo(
    () => editable
      ? calculateQuote({
          currency: draft.currency,
          taxType: draft.taxType,
          subtotalAmount,
          discountAmount: draft.discountAmount,
          shippingAmount: draft.shippingAmount,
          otherAmount: draft.otherAmount,
        })
      : { supplyAmount: revision.supplyAmount, taxAmount: revision.taxAmount, totalAmount: revision.totalAmount },
    [
      draft.currency,
      draft.taxType,
      subtotalAmount,
      draft.discountAmount,
      draft.shippingAmount,
      draft.otherAmount,
      editable,
      revision.supplyAmount,
      revision.taxAmount,
      revision.totalAmount,
    ],
  );
  const [productId, setProductId] = useState("");
  const activeProducts = products.filter(
    (product) => product.status === "active",
  );
  const dirty = JSON.stringify(draft) !== JSON.stringify(toInput(revision));
  const validation =
    draft.lines.length === 0
      ? "견적 품목을 하나 이상 등록해 주세요."
      : draft.lines.some(
            (line) =>
              !Number.isFinite(line.quantity) ||
              line.quantity <= 0 ||
              !Number.isFinite(line.unitPrice) ||
              line.unitPrice < 0,
          )
        ? "수량은 0보다 커야 하고 단가는 0 이상이어야 합니다."
        : validateQuoteDraft(draft);
  const addLine = () => {
    const product = activeProducts.find((item) => item.id === productId);
    if (!product) return;
    setDraft((current) => ({
      ...current,
      lines: [
        ...current.lines,
        {
          productId: product.id,
          description: product.name,
          quantity: 1,
          unitPrice: 0,
        },
      ],
    }));
  };
  const updateLine = (
    index: number,
    key: "quantity" | "unitPrice",
    value: number,
  ) =>
    setDraft((current) => ({
      ...current,
      lines: current.lines.map((line, lineIndex) =>
        lineIndex === index ? { ...line, [key]: value } : line,
      ),
    }));

  return (
    <article className="quote-detail">
      <div className="detail-heading">
        <div>
          <p>{quote.quoteNumber}</p>
          <h2>
            {quote.customerName} · {revision.revisionLabel}
          </h2>
        </div>
        <span className="status-badge">{revision.status}</span>
      </div>
      <div className="quote-terms">
        <label>
          유효기간
          <input
            aria-label="유효기간"
            type="date"
            disabled={!editable}
            value={draft.validUntil}
            onChange={(event) =>
              setDraft((current) => ({ ...current, validUntil: event.target.value }))
            }
          />
        </label>
        <label>
          거래구분
          <select
            disabled={!editable}
            value={draft.tradeType}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                tradeType: event.target
                  .value as QuoteRevisionInput["tradeType"],
                incotermCode:
                  event.target.value === "domestic"
                    ? null
                    : current.incotermCode ||
                      incoterms.find((item) => item.status === "active")
                        ?.code ||
                      null,
                namedPlace:
                  event.target.value === "domestic" ? null : current.namedPlace,
              }))
            }
          >
            <option value="domestic">국내</option>
            <option value="export">수출</option>
          </select>
        </label>
        <label>
          통화
          <select
            disabled={!editable}
            value={draft.currency}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                currency: event.target.value as QuoteRevisionInput["currency"],
              }))
            }
          >
            <option>KRW</option>
            <option>USD</option>
            <option>CNY</option>
          </select>
        </label>
        <label>
          세금
          <select
            disabled={!editable}
            value={draft.taxType}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                taxType: event.target.value as QuoteRevisionInput["taxType"],
              }))
            }
          >
            <option value="exclusive">VAT 별도</option>
            <option value="inclusive">VAT 포함</option>
            <option value="exempt">면세</option>
          </select>
        </label>
        {draft.tradeType === "export" && (
          <>
            <label>
              Incoterms
              <select
                disabled={!editable}
                value={draft.incotermCode ?? ""}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    incotermCode: event.target.value,
                  }))
                }
              >
                {incoterms
                  .filter((item) => item.status === "active")
                  .map((item) => (
                    <option key={item.id} value={item.code}>
                      {item.code}
                    </option>
                  ))}
              </select>
            </label>
            <label>
              지정 장소
              <input
                disabled={!editable}
                value={draft.namedPlace ?? ""}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    namedPlace: event.target.value,
                  }))
                }
              />
            </label>
          </>
        )}
      </div>
      <>
        {editable && (
          <div className="detail-actions">
            <label>
              추가할 품목
              <select
                aria-label="추가할 품목"
                disabled={busy}
                value={productId}
                onChange={(event) => setProductId(event.target.value)}
              >
                <option value="">품목 선택</option>
                {activeProducts.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.code} · {product.name} ·{" "}
                    {product.itemType === "product" ? "제품" : "상품"}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              disabled={
                busy || !activeProducts.some((item) => item.id === productId)
              }
              onClick={addLine}
            >
              품목 추가
            </button>
          </div>
        )}
      </>
      <table>
        <thead>
          <tr>
            <th>품목</th>
            <th>수량</th>
            <th>단가</th>
            <th>금액</th>
            {editable && <th>작업</th>}
          </tr>
        </thead>
        <tbody>
          {draft.lines.map((line, index) => (
            <tr key={`${line.productId}-${index}`}>
              <td>{line.description}</td>
              <td>
                {editable ? (
                  <input
                    aria-label={`${line.description} 수량`}
                    type="number"
                    min="1"
                    value={line.quantity}
                    onChange={(event) =>
                      updateLine(index, "quantity", Number(event.target.value))
                    }
                  />
                ) : (
                  line.quantity
                )}
              </td>
              <td>
                {editable ? (
                  <input
                    aria-label={`${line.description} 단가`}
                    type="number"
                    min="0"
                    step="any"
                    value={line.unitPrice}
                    onChange={(event) =>
                      updateLine(index, "unitPrice", Number(event.target.value))
                    }
                  />
                ) : (
                  line.unitPrice.toLocaleString()
                )}
              </td>
              <td>{(editable ? calculateQuoteLineAmount(draft.currency, line.quantity, line.unitPrice) : revision.lines[index]?.amount ?? 0).toLocaleString()}</td>
              {editable && (
                <td>
                  <button
                    type="button"
                    disabled={busy}
                    aria-label={`${index + 1}번 품목 삭제`}
                    onClick={() =>
                      setDraft((current) => ({
                        ...current,
                        lines: current.lines.filter(
                          (_, rowIndex) => rowIndex !== index,
                        ),
                      }))
                    }
                  >
                    삭제
                  </button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
      <div className="quote-costs">
        <label>
          할인
          <input
            disabled={!editable}
            type="number"
            min="0"
            value={draft.discountAmount}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                discountAmount: Number(event.target.value),
              }))
            }
          />
        </label>
        <label>
          운송비
          <input
            disabled={!editable}
            type="number"
            min="0"
            value={draft.shippingAmount}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                shippingAmount: Number(event.target.value),
              }))
            }
          />
        </label>
        <label>
          기타비용
          <input
            disabled={!editable}
            type="number"
            min="0"
            value={draft.otherAmount}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                otherAmount: Number(event.target.value),
              }))
            }
          />
        </label>
        {editable && (
          <button
            type="button"
            disabled={busy || Boolean(validation)}
            onClick={() => onSave(draft)}
          >
            견적 저장
          </button>
        )}
      </div>
      {validation && <p role="alert">{validation}</p>}
      <dl className="quote-totals">
        <div>
          <dt>공급가액</dt>
          <dd>{totals.supplyAmount.toLocaleString()}</dd>
        </div>
        <div>
          <dt>세액</dt>
          <dd>{totals.taxAmount.toLocaleString()}</dd>
        </div>
        <div>
          <dt>최종 견적금액</dt>
          <dd>
            {draft.currency} {totals.totalAmount.toLocaleString()}
          </dd>
        </div>
      </dl>
      <QuoteCommercialTerms
        values={draft}
        disabled={!editable || busy}
        onChange={(field, value) =>
          setDraft((current) => ({ ...current, [field]: value }))
        }
      />
      <QuoteApprovalPanel
        quote={quote}
        user={user}
        busy={busy || Boolean(validation) || dirty}
        onSubmit={onSubmit}
        onApprove={onApprove}
        onReject={onReject}
        onNewRevision={onNewRevision}
        onPdf={onPdf}
        onDispatch={onDispatch}
        onResult={onResult}
      />
    </article>
  );
}

function toInput(revision: QuoteDto["currentRevision"]): QuoteRevisionInput {
  return {
    tradeType: revision.tradeType,
    currency: revision.currency,
    taxType: revision.taxType,
    validUntil: revision.validUntil,
    incotermCode: revision.incotermCode,
    namedPlace: revision.namedPlace,
    paymentTerms: revision.paymentTerms ?? "",
    deliveryTerms: revision.deliveryTerms ?? "",
    notes: revision.notes ?? "",
    discountAmount: revision.discountAmount,
    shippingAmount: revision.shippingAmount,
    otherAmount: revision.otherAmount,
    lines: revision.lines.map(
      ({ productId, description, quantity, unitPrice }) => ({
        productId,
        description,
        quantity,
        unitPrice,
      }),
    ),
    expectedVersion: revision.version,
  };
}
