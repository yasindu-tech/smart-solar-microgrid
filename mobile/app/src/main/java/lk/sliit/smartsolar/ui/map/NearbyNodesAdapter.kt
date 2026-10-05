/**
 * File        : NearbyNodesAdapter.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.map
 * Description : RecyclerView adapter for nearby active microgrid nodes list
 *               displayed below the map view on MapActivity.
 */

package lk.sliit.smartsolar.ui.map

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.dto.NodeResponse
import lk.sliit.smartsolar.databinding.ItemNearbyNodeBinding

class NearbyNodesAdapter(
    private val onNodeClick: (NodeResponse) -> Unit
) : RecyclerView.Adapter<NearbyNodesAdapter.NearbyNodeViewHolder>() {

    private val items = mutableListOf<NodeResponse>()

    fun submitList(nodes: List<NodeResponse>) {
        items.clear()
        items.addAll(nodes)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): NearbyNodeViewHolder {
        val binding = ItemNearbyNodeBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return NearbyNodeViewHolder(binding, onNodeClick)
    }

    override fun onBindViewHolder(holder: NearbyNodeViewHolder, position: Int) {
        holder.bind(items[position])
    }

    override fun getItemCount(): Int = items.size

    class NearbyNodeViewHolder(
        private val binding: ItemNearbyNodeBinding,
        private val onNodeClick: (NodeResponse) -> Unit
    ) : RecyclerView.ViewHolder(binding.root) {

        fun bind(node: NodeResponse) {
            val context = binding.root.context

            binding.tvNodeName.text = node.name ?: context.getString(
                R.string.dashboard_node_label,
                node.nodeId.orEmpty()
            )

            val distanceStr = node.distanceKm?.let {
                context.getString(R.string.map_nearby_distance_format, it)
            }
            val slotsStr = if (node.batterySlotCount > 0) {
                context.getString(R.string.map_nearby_slots_format, node.batterySlotCount)
            } else null

            val capacityStr = if (node.capacityKwh > 0.0) {
                context.getString(R.string.map_nearby_capacity_format, node.capacityKwh)
            } else null

            binding.tvNodeDetails.text = listOfNotNull(distanceStr, slotsStr, capacityStr)
                .joinToString(" · ")

            binding.root.setOnClickListener {
                onNodeClick(node)
            }
        }
    }
}
