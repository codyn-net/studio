require 'serialize'

module Cpg
	class Allocation < Array
		include Serialize
	
		property :x, :y, :width, :height
	
		def initialize(ini = nil)
			super(ini == nil ? [0, 0, 1, 1] : ini)
		end
	
		def property_set?(name)
			true
		end
	
		def get_property(name)
			send(name)
		end
	
		def set_property(name, val)
			send("#{name}=", val.to_f)
		end
	
		def x
			self[0]
		end
	
		def x=(val)
			self[0] = val
		end
	
		def y
			self[1]
		end
	
		def y=(val)
			self[1] = val 
		end
	
		def width
			self[2]
		end
	
		def width=(val)
			self[2] = val 
		end
	
		def height
			self[3]
		end
	
		def height=(val)
			self[3] = val 
		end
	
		def *(some)
			Allocation.new(map { |x| x * some })
		end
	
		def -(some)
			Allocation.new(map { |x| x - some })
		end
	
		def +(some)
			Allocation.new(map { |x| x + some })
		end
	
		def /(some)
			Allocation.new(map { |x| x / some })
		end
	
		def move(x, y)
			self.x = x
			self.y = y
		end
	
		def move_rel(x, y)
			self.x += x
			self.y += y
		end
	end
end
